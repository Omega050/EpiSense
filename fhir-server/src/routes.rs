use crate::db::Database;
use crate::forwarder::Forwarder;
use crate::models::FhirMessage;
use actix_web::{web, HttpResponse, Responder};
use bytes::Bytes;
use serde::{Deserialize, Serialize};
use tracing::{debug, error, info};

/// Estado compartilhado da aplicação
pub struct AppState {
    pub db: Database,
    pub forwarder: Forwarder,
}

/// Resposta padrão da API
#[derive(Serialize)]
struct ApiResponse {
    success: bool,
    message: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    id: Option<String>,
}

/// Health check endpoint
pub async fn health_check(data: web::Data<AppState>) -> impl Responder {
    match data.db.health_check().await {
        Ok(_) => {
            debug!("Health check passou");
            HttpResponse::Ok().json(ApiResponse {
                success: true,
                message: "Service is healthy".to_string(),
                id: None,
            })
        }
        Err(e) => {
            error!("Health check falhou: {}", e);
            HttpResponse::ServiceUnavailable().json(ApiResponse {
                success: false,
                message: format!("Database health check failed: {}", e),
                id: None,
            })
        }
    }
}

/// Endpoint principal para receber dados FHIR
/// Aceita JSON bruto e processa de forma assíncrona
pub async fn receive_fhir(
    payload: Bytes,
    data: web::Data<AppState>,
) -> impl Responder {
    // Converte bytes para string (validação mínima de UTF-8)
    let payload_str = match String::from_utf8(payload.to_vec()) {
        Ok(s) => s,
        Err(e) => {
            error!("Payload inválido (não é UTF-8): {}", e);
            return HttpResponse::BadRequest().json(ApiResponse {
                success: false,
                message: "Invalid payload: must be valid UTF-8".to_string(),
                id: None,
            });
        }
    };

    // Validação básica: verifica se é JSON válido (sem parsing completo)
    if serde_json::from_str::<serde_json::Value>(&payload_str).is_err() {
        error!("Payload inválido (não é JSON válido)");
        return HttpResponse::BadRequest().json(ApiResponse {
            success: false,
            message: "Invalid payload: must be valid JSON".to_string(),
            id: None,
        });
    }

    // Cria nova mensagem
    let message = FhirMessage::new(payload_str);
    let message_id = message.id;

    info!("📩 Recebida nova mensagem FHIR: {}", message_id);
    info!("🔧 Preparando para inserir mensagem no banco...");

    // Insere no banco de forma assíncrona
    match data.db.insert_message(&message).await {
        Ok(_) => {
            info!("✅ Mensagem {} salva com sucesso no ScyllaDB!", message_id);
        },
        Err(e) => {
            error!("❌ ERRO ao salvar mensagem {}: {:?}", message_id, e);
            error!("❌ ERRO tipo: {}", e);
            return HttpResponse::InternalServerError().json(ApiResponse {
                success: false,
                message: format!("Failed to store message: {}", e),
                id: None,
            });
        }
    }

    // Envia ao backend de forma não-bloqueante
    // Clona os dados necessários para evitar referências
    let db_clone = data.db.clone();
    let forwarder_clone = data.forwarder.clone();
    let message_clone = message.clone();

    tokio::spawn(async move {
        if let Err(e) = forwarder_clone.forward_message(&message_clone, &db_clone).await {
            error!("Erro ao encaminhar mensagem {}: {}", message_id, e);
        }
    });

    debug!("Mensagem {} armazenada e agendada para envio", message_id);

    HttpResponse::Accepted().json(ApiResponse {
        success: true,
        message: "Message received and queued for processing".to_string(),
        id: Some(message_id.to_string()),
    })
}

/// Endpoint para consultar estatísticas
#[derive(Serialize)]
struct StatsResponse {
    pending: i64,
    sent: i64,
    failed: i64,
}

pub async fn get_stats(data: web::Data<AppState>) -> impl Responder {
    let pending = data.db.count_by_status("pending").await.unwrap_or(0);
    let sent = data.db.count_by_status("sent").await.unwrap_or(0);
    let failed = data.db.count_by_status("failed").await.unwrap_or(0);

    HttpResponse::Ok().json(StatsResponse {
        pending,
        sent,
        failed,
    })
}

/// Configuração das rotas
pub fn configure_routes(cfg: &mut web::ServiceConfig) {
    cfg.service(
        web::scope("/api")
            .route("/health", web::get().to(health_check))
            .route("/fhir", web::post().to(receive_fhir))
            .route("/stats", web::get().to(get_stats)),
    );
}
