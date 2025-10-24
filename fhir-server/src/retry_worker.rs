use crate::config::RetryWorkerConfig;
use crate::db::Database;
use crate::forwarder::Forwarder;
use anyhow::Result;
use std::sync::Arc;
use tokio::time::{interval, Duration};
use tracing::{debug, error, info, warn};

/// Worker responsável por reenviar mensagens pendentes/falhas
pub struct RetryWorker {
    db: Database,
    forwarder: Forwarder,
    config: RetryWorkerConfig,
}

impl RetryWorker {
    pub fn new(db: Database, forwarder: Forwarder, config: RetryWorkerConfig) -> Self {
        Self {
            db,
            forwarder,
            config,
        }
    }

    /// Inicia o worker em background
    pub async fn start(self) {
        info!(
            "Iniciando Retry Worker (intervalo: {}s, batch: {})",
            self.config.interval_secs, self.config.batch_size
        );

        let mut ticker = interval(self.config.interval());

        loop {
            ticker.tick().await;
            
            debug!("Executando ciclo de retry...");
            
            if let Err(e) = self.process_retry_batch().await {
                error!("Erro durante processamento de retry: {}", e);
            }
        }
    }

    /// Processa um lote de mensagens para retry
    async fn process_retry_batch(&self) -> Result<()> {
        // Busca mensagens pendentes ou com falha
        let messages = self
            .db
            .get_messages_for_retry(self.config.batch_size)
            .await?;

        if messages.is_empty() {
            debug!("Nenhuma mensagem para reenviar");
            return Ok(());
        }

        info!("Reenviando {} mensagens", messages.len());

        // Filtra mensagens que ainda podem ser retentadas
        let retryable: Vec<_> = messages
            .into_iter()
            .filter(|msg| {
                // Limita número de tentativas (configurável via BACKEND_MAX_RETRIES)
                // Aqui usamos um limite hard-coded adicional para evitar loops infinitos
                if msg.retry_count >= 10 {
                    warn!(
                        "Mensagem {} excedeu limite de tentativas ({}), ignorando",
                        msg.id, msg.retry_count
                    );
                    false
                } else {
                    true
                }
            })
            .collect();

        if retryable.is_empty() {
            debug!("Nenhuma mensagem elegível para retry após filtragem");
            return Ok(());
        }

        // Envia em batch
        let results = self.forwarder.forward_batch(retryable, &self.db).await;

        let successes = results.iter().filter(|r| r.is_ok()).count();
        let failures = results.len() - successes;

        info!(
            "Batch de retry concluído: {} sucessos, {} falhas",
            successes, failures
        );

        Ok(())
    }
}
