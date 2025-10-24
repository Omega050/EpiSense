use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

/// Status possíveis de uma mensagem FHIR
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "lowercase")]
pub enum MessageStatus {
    Pending,
    Sent,
    Failed,
}

impl MessageStatus {
    pub fn as_str(&self) -> &str {
        match self {
            MessageStatus::Pending => "pending",
            MessageStatus::Sent => "sent",
            MessageStatus::Failed => "failed",
        }
    }
}

impl From<String> for MessageStatus {
    fn from(s: String) -> Self {
        match s.as_str() {
            "sent" => MessageStatus::Sent,
            "failed" => MessageStatus::Failed,
            _ => MessageStatus::Pending,
        }
    }
}

/// Representa uma mensagem FHIR armazenada no banco
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FhirMessage {
    pub id: Uuid,
    pub payload: String, // JSON bruto como string
    pub status: MessageStatus,
    pub received_at: DateTime<Utc>,
    pub sent_at: Option<DateTime<Utc>>,
    pub retry_count: i32,
    pub last_error: Option<String>,
    pub last_retry_at: Option<DateTime<Utc>>,
}

impl FhirMessage {
    /// Cria uma nova mensagem com status pending
    pub fn new(payload: String) -> Self {
        Self {
            id: Uuid::new_v4(),
            payload,
            status: MessageStatus::Pending,
            received_at: Utc::now(),
            sent_at: None,
            retry_count: 0,
            last_error: None,
            last_retry_at: None,
        }
    }

    /// Marca a mensagem como enviada com sucesso
    pub fn mark_sent(&mut self) {
        self.status = MessageStatus::Sent;
        self.sent_at = Some(Utc::now());
    }

    /// Marca a mensagem como falha e registra o erro
    pub fn mark_failed(&mut self, error: String) {
        self.status = MessageStatus::Failed;
        self.retry_count += 1;
        self.last_error = Some(error);
        self.last_retry_at = Some(Utc::now());
    }
}

/// Estatísticas de processamento (para observabilidade)
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ProcessingStats {
    pub total_received: u64,
    pub total_sent: u64,
    pub total_failed: u64,
    pub pending_count: u64,
    pub avg_latency_ms: f64,
}
