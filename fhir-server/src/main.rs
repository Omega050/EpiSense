use actix_web::{middleware, web, App, HttpServer};
use fhir_server::{
    config::Config, db::Database, forwarder::Forwarder, metrics, retry_worker::RetryWorker,
    routes,
};
use tracing::{error, info};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    // Inicializa sistema de logging
    tracing_subscriber::registry()
        .with(
            tracing_subscriber::EnvFilter::try_from_default_env()
                .unwrap_or_else(|_| "info,fhir_server=debug".into()),
        )
        .with(tracing_subscriber::fmt::layer())
        .init();

    info!("🚀 Iniciando FHIR Server...");

    // Carrega configuração
    let config = Config::from_env().expect("Falha ao carregar configuração");
    info!("Configuração carregada com sucesso");

    // Inicializa métricas Prometheus
    metrics::init_metrics().expect("Falha ao inicializar métricas");
    info!("Métricas Prometheus inicializadas");

    // Conecta ao ScyllaDB
    info!("Conectando ao ScyllaDB...");
    let db = Database::new(&config.scylla)
        .await
        .expect("Falha ao conectar ao ScyllaDB");
    info!("✓ Conectado ao ScyllaDB");

    // Cria o forwarder
    let forwarder = Forwarder::new(&config.backend).expect("Falha ao criar Forwarder");
    info!("✓ Forwarder configurado");

    // Inicia Retry Worker em background
    let retry_worker = RetryWorker::new(
        db.clone(),
        forwarder.clone(),
        config.retry_worker.clone(),
    );
    
    tokio::spawn(async move {
        retry_worker.start().await;
    });
    info!("✓ Retry Worker iniciado");

    // Configura servidor HTTP
    let bind_addr = format!("{}:{}", config.host, config.port);
    info!("Iniciando servidor HTTP em {}", bind_addr);

    let server = HttpServer::new(move || {
        App::new()
            // Estado compartilhado
            .app_data(web::Data::new(routes::AppState {
                db: db.clone(),
                forwarder: forwarder.clone(),
            }))
            // Middlewares
            .wrap(middleware::Logger::default())
            .wrap(middleware::Compress::default())
            // Configuração de payload
            .app_data(web::PayloadConfig::new(10 * 1024 * 1024)) // 10MB max
            // Rotas
            .configure(routes::configure_routes)
            // Endpoint de métricas
            .route("/metrics", web::get().to(metrics::metrics_handler))
    })
    .workers(config.performance.workers)
    .keep_alive(config.performance.keep_alive())
    .bind(&bind_addr)?;

    info!("✓ Servidor HTTP pronto e escutando em {}", bind_addr);
    info!("📊 Métricas disponíveis em: http://{}/metrics", bind_addr);
    info!("🏥 Health check: http://{}/api/health", bind_addr);
    info!("📨 FHIR endpoint: http://{}/api/fhir", bind_addr);

    server.run().await
}
