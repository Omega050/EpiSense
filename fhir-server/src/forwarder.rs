use crate::config::BackendConfig;
use crate::db::Database;
use crate::models::FhirMessage;
use anyhow::{Context, Result};
use reqwest::{Client, StatusCode};
use std::time::Duration;
use tokio_retry::strategy::{jitter, ExponentialBackoff};
use tokio_retry::Retry;
use tracing::{debug, error, info, warn};

/// Serviço responsável por encaminhar mensagens ao backend
#[derive(Clone)]
pub struct Forwarder {
    client: Client,
    backend_url: String,
    max_retries: usize,
    initial_backoff_ms: u64,
    max_backoff_ms: u64,
}

impl Forwarder {
    /// Cria uma nova instância do Forwarder
    pub fn new(config: &BackendConfig) -> Result<Self> {
        let client = Client::builder()
            .timeout(config.timeout())
            .pool_max_idle_per_host(50)
            .pool_idle_timeout(Some(Duration::from_secs(90)))
            .tcp_keepalive(Some(Duration::from_secs(60)))
            .build()
            .context("Falha ao criar HTTP client")?;

        info!("Forwarder configurado para: {}", config.url);

        Ok(Self {
            client,
            backend_url: config.url.clone(),
            max_retries: config.max_retries as usize,
            initial_backoff_ms: config.initial_backoff_ms,
            max_backoff_ms: config.max_backoff().as_millis() as u64,
        })
    }

    /// Encaminha uma mensagem ao backend com retry automático
    pub async fn forward_message(&self, message: &FhirMessage, db: &Database) -> Result<()> {
        let message_id = message.id;
        let payload = message.payload.clone();

        debug!("Iniciando envio da mensagem {} ao backend", message_id);

        // Cria a estratégia de retry para esta tentativa
        let retry_strategy = ExponentialBackoff::from_millis(self.initial_backoff_ms)
            .max_delay(Duration::from_millis(self.max_backoff_ms))
            .take(self.max_retries)
            .map(jitter);

        // Tenta enviar com retry
        let result = Retry::spawn(retry_strategy, || async {
            self.send_to_backend(&payload).await
        })
        .await;

        match result {
            Ok(_) => {
                info!("Mensagem {} enviada com sucesso", message_id);
                db.mark_as_sent(message_id)
                    .await
                    .context("Falha ao atualizar status para 'sent'")?;
                Ok(())
            }
            Err(e) => {
                error!("Falha ao enviar mensagem {} após {} tentativas: {}", 
                    message_id, self.max_retries, e);
                
                let error_msg = format!("{}", e);
                let retry_count = message.retry_count + 1;
                
                db.mark_as_failed(message_id, &error_msg, retry_count)
                    .await
                    .context("Falha ao atualizar status para 'failed'")?;
                
                Err(e)
            }
        }
    }

    /// Envia o payload ao backend (tentativa única)
    async fn send_to_backend(&self, payload: &str) -> Result<()> {
        let response = self
            .client
            .post(&self.backend_url)
            .header("Content-Type", "application/json")
            .body(payload.to_string())
            .send()
            .await
            .context("Falha ao enviar requisição ao backend")?;

        let status = response.status();

        if status.is_success() {
            debug!("Backend respondeu com status: {}", status);
            Ok(())
        } else {
            let error_body = response
                .text()
                .await
                .unwrap_or_else(|_| "Unable to read error body".to_string());

            warn!("Backend retornou erro: {} - {}", status, error_body);

            // Não faz retry em erros 4xx (exceto 429 - Too Many Requests)
            if status.is_client_error() && status != StatusCode::TOO_MANY_REQUESTS {
                anyhow::bail!(
                    "Client error (não será retentado): {} - {}",
                    status,
                    error_body
                );
            }

            anyhow::bail!("Backend error: {} - {}", status, error_body)
        }
    }

    /// Envia múltiplas mensagens em batch (para retry worker)
    pub async fn forward_batch(
        &self,
        messages: Vec<FhirMessage>,
        db: &Database,
    ) -> Vec<Result<()>> {
        let tasks: Vec<_> = messages
            .into_iter()
            .map(|msg| {
                let forwarder = self.clone();
                let db = db.clone();
                tokio::spawn(async move { forwarder.forward_message(&msg, &db).await })
            })
            .collect();

        let mut results = Vec::new();
        for task in tasks {
            match task.await {
                Ok(result) => results.push(result),
                Err(e) => results.push(Err(anyhow::anyhow!("Task join error: {}", e))),
            }
        }

        results
    }
}
