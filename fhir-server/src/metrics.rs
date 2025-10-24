use actix_web::{web, HttpResponse, Responder};
use prometheus::{
    Encoder, HistogramOpts, HistogramVec, IntCounter, IntCounterVec, IntGauge, Opts, Registry,
    TextEncoder,
};
use std::sync::Arc;
use tracing::error;

lazy_static::lazy_static! {
    /// Registry global do Prometheus
    pub static ref REGISTRY: Registry = Registry::new();

    /// Contador de mensagens recebidas
    pub static ref MESSAGES_RECEIVED: IntCounter = IntCounter::new(
        "fhir_messages_received_total",
        "Total number of FHIR messages received"
    ).unwrap();

    /// Contador de mensagens enviadas com sucesso
    pub static ref MESSAGES_SENT: IntCounter = IntCounter::new(
        "fhir_messages_sent_total",
        "Total number of FHIR messages successfully sent to backend"
    ).unwrap();

    /// Contador de mensagens com falha
    pub static ref MESSAGES_FAILED: IntCounter = IntCounter::new(
        "fhir_messages_failed_total",
        "Total number of FHIR messages that failed to send"
    ).unwrap();

    /// Contador por status HTTP da resposta do backend
    pub static ref BACKEND_RESPONSE_STATUS: IntCounterVec = IntCounterVec::new(
        Opts::new(
            "backend_response_status_total",
            "Total backend responses by HTTP status code"
        ),
        &["status_code"]
    ).unwrap();

    /// Histograma de latência de processamento
    pub static ref PROCESSING_LATENCY: HistogramVec = HistogramVec::new(
        HistogramOpts::new(
            "fhir_processing_latency_seconds",
            "Latency of FHIR message processing"
        ).buckets(vec![0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0]),
        &["operation"]
    ).unwrap();

    /// Gauge de mensagens pendentes
    pub static ref MESSAGES_PENDING: IntGauge = IntGauge::new(
        "fhir_messages_pending",
        "Number of FHIR messages pending to be sent"
    ).unwrap();

    /// Histograma de tamanho de payload
    pub static ref PAYLOAD_SIZE: HistogramVec = HistogramVec::new(
        HistogramOpts::new(
            "fhir_payload_size_bytes",
            "Size of FHIR payloads in bytes"
        ).buckets(vec![
            100.0, 500.0, 1000.0, 5000.0, 10000.0, 50000.0, 100000.0, 500000.0, 1000000.0
        ]),
        &["direction"]
    ).unwrap();

    /// Contador de tentativas de retry
    pub static ref RETRY_ATTEMPTS: IntCounter = IntCounter::new(
        "fhir_retry_attempts_total",
        "Total number of retry attempts"
    ).unwrap();
}

/// Inicializa métricas do Prometheus
pub fn init_metrics() -> Result<(), prometheus::Error> {
    REGISTRY.register(Box::new(MESSAGES_RECEIVED.clone()))?;
    REGISTRY.register(Box::new(MESSAGES_SENT.clone()))?;
    REGISTRY.register(Box::new(MESSAGES_FAILED.clone()))?;
    REGISTRY.register(Box::new(BACKEND_RESPONSE_STATUS.clone()))?;
    REGISTRY.register(Box::new(PROCESSING_LATENCY.clone()))?;
    REGISTRY.register(Box::new(MESSAGES_PENDING.clone()))?;
    REGISTRY.register(Box::new(PAYLOAD_SIZE.clone()))?;
    REGISTRY.register(Box::new(RETRY_ATTEMPTS.clone()))?;
    Ok(())
}

/// Endpoint para expor métricas Prometheus
pub async fn metrics_handler() -> impl Responder {
    let encoder = TextEncoder::new();
    let metric_families = REGISTRY.gather();
    
    let mut buffer = vec![];
    match encoder.encode(&metric_families, &mut buffer) {
        Ok(_) => HttpResponse::Ok()
            .content_type("text/plain; version=0.0.4")
            .body(buffer),
        Err(e) => {
            error!("Erro ao encodar métricas: {}", e);
            HttpResponse::InternalServerError().body("Failed to encode metrics")
        }
    }
}

/// Macro helper para medir latência de operações
#[macro_export]
macro_rules! measure_latency {
    ($operation:expr, $code:block) => {{
        let timer = $crate::metrics::PROCESSING_LATENCY
            .with_label_values(&[$operation])
            .start_timer();
        let result = $code;
        timer.observe_duration();
        result
    }};
}
