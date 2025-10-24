use crate::config::ScyllaConfig;
use crate::models::{FhirMessage, MessageStatus};
use anyhow::{Context, Result};
use chrono::{DateTime, Utc};
use scylla::frame::value::CqlTimestamp;
use scylla::{Session, SessionBuilder};
use std::sync::Arc;
use tracing::{debug, error, info, warn};
use uuid::Uuid;

/// Cliente do banco de dados ScyllaDB
#[derive(Clone)]
pub struct Database {
    session: Arc<Session>,
    keyspace: String,
}

impl Database {
    /// Cria uma nova conexão com o ScyllaDB
    pub async fn new(config: &ScyllaConfig) -> Result<Self> {
        info!("Conectando ao ScyllaDB em: {}", config.nodes);

        let mut builder = SessionBuilder::new()
            .known_nodes(&config.node_list())
            .compression(Some(scylla::transport::Compression::Lz4));

        // Adiciona credenciais se fornecidas
        if let (Some(username), Some(password)) = (&config.username, &config.password) {
            builder = builder.user(username, password);
        }

        let session = builder
            .build()
            .await
            .context("Falha ao conectar ao ScyllaDB")?;

        info!("Conexão com ScyllaDB estabelecida com sucesso");

        Ok(Self {
            session: Arc::new(session),
            keyspace: config.keyspace.clone(),
        })
    }

    /// Insere uma nova mensagem FHIR no banco
    pub async fn insert_message(&self, message: &FhirMessage) -> Result<()> {
        let query = format!(
            "INSERT INTO {}.fhir_messages (id, payload, status, received_at, sent_at, retry_count, last_error, last_retry_at) \
             VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            self.keyspace
        );
        
        info!("🔍 Tentando inserir mensagem {} no ScyllaDB", message.id);
        info!("📝 Query: {}", query);
        
        let values = (
            message.id,
            &message.payload,
            message.status.as_str(),
            CqlTimestamp(message.received_at.timestamp_millis()),
            message.sent_at.map(|t| CqlTimestamp(t.timestamp_millis())),
            message.retry_count,
            &message.last_error,
            message.last_retry_at.map(|t| CqlTimestamp(t.timestamp_millis())),
        );
        
        info!("✅ Valores preparados: id={}, status={}, received_at={}", message.id, message.status.as_str(), message.received_at.timestamp_millis());
        
        match self.session.query(query, values).await {
            Ok(_) => {
                info!("✅ Mensagem {} inserida com sucesso no ScyllaDB!", message.id);
                Ok(())
            },
            Err(e) => {
                error!("❌ ERRO COMPLETO do Scylla: {:#?}", e);
                Err(anyhow::anyhow!("Falha ao inserir mensagem: {:?}", e))
            }
        }
    }

    /// Atualiza o status de uma mensagem para 'sent'
    pub async fn mark_as_sent(&self, id: Uuid) -> Result<()> {
        let query = format!(
            "UPDATE {}.fhir_messages SET status = ?, sent_at = ? WHERE id = ?",
            self.keyspace
        );
        
        self.session
            .query(query, ("sent", CqlTimestamp(Utc::now().timestamp_millis()), id))
            .await
            .context("Falha ao marcar mensagem como enviada")?;

        debug!("Mensagem {} marcada como enviada", id);
        Ok(())
    }

    /// Atualiza o status de uma mensagem para 'failed'
    pub async fn mark_as_failed(&self, id: Uuid, error: &str, retry_count: i32) -> Result<()> {
        let query = format!(
            "UPDATE {}.fhir_messages SET status = ?, retry_count = ?, last_error = ?, last_retry_at = ? WHERE id = ?",
            self.keyspace
        );
        
        self.session
            .query(query, ("failed", retry_count, error, CqlTimestamp(Utc::now().timestamp_millis()), id))
            .await
            .context("Falha ao marcar mensagem como falha")?;

        debug!("Mensagem {} marcada como falha (tentativa {})", id, retry_count);
        Ok(())
    }

    /// Busca mensagens para retry (status = 'pending' ou 'failed')
    pub async fn get_messages_for_retry(&self, limit: i32) -> Result<Vec<FhirMessage>> {
        let query = format!(
            "SELECT id, payload, status, received_at, sent_at, retry_count, last_error, last_retry_at \
             FROM {}.fhir_messages WHERE status IN ('pending', 'failed') LIMIT ? ALLOW FILTERING",
            self.keyspace
        );

        let rows = self
            .session
            .query(query, (limit,))
            .await
            .context("Falha ao buscar mensagens para retry")?
            .rows
            .unwrap_or_default();

        let mut messages = Vec::with_capacity(rows.len());

        for row in rows {
            match self.parse_message_row(row) {
                Ok(msg) => messages.push(msg),
                Err(e) => warn!("Erro ao parsear mensagem: {}", e),
            }
        }

        debug!("Encontradas {} mensagens para retry", messages.len());
        Ok(messages)
    }

    /// Obtém uma mensagem pelo ID
    pub async fn get_message_by_id(&self, id: Uuid) -> Result<Option<FhirMessage>> {
        let query = format!(
            "SELECT id, payload, status, received_at, sent_at, retry_count, last_error, last_retry_at \
             FROM {}.fhir_messages WHERE id = ?",
            self.keyspace
        );

        let rows = self
            .session
            .query(query, (id,))
            .await
            .context("Falha ao buscar mensagem por ID")?
            .rows
            .unwrap_or_default();

        if let Some(row) = rows.into_iter().next() {
            Ok(Some(self.parse_message_row(row)?))
        } else {
            Ok(None)
        }
    }

    /// Conta mensagens por status
    pub async fn count_by_status(&self, status: &str) -> Result<i64> {
        let query = format!(
            "SELECT COUNT(*) FROM {}.fhir_messages WHERE status = ? ALLOW FILTERING",
            self.keyspace
        );

        let rows = self
            .session
            .query(query, (status,))
            .await
            .context("Falha ao contar mensagens")?
            .rows
            .unwrap_or_default();

        if let Some(row) = rows.into_iter().next() {
            let count: i64 = row.columns[0]
                .as_ref()
                .and_then(|v| v.as_bigint())
                .unwrap_or(0);
            Ok(count)
        } else {
            Ok(0)
        }
    }

    /// Parseia uma linha do banco para FhirMessage
    fn parse_message_row(&self, row: scylla::frame::response::result::Row) -> Result<FhirMessage> {
        let id: Uuid = row.columns[0]
            .as_ref()
            .and_then(|v| v.as_uuid())
            .context("Campo 'id' inválido")?;

        let payload: String = row.columns[1]
            .as_ref()
            .and_then(|v| v.as_text())
            .context("Campo 'payload' inválido")?
            .to_string();

        let status: String = row.columns[2]
            .as_ref()
            .and_then(|v| v.as_text())
            .context("Campo 'status' inválido")?
            .to_string();

        let received_at: DateTime<Utc> = row.columns[3]
            .as_ref()
            .and_then(|v| v.as_cql_timestamp())
            .and_then(|ts| DateTime::from_timestamp_millis(ts.0))
            .context("Campo 'received_at' inválido")?;

        let sent_at: Option<DateTime<Utc>> = row.columns[4]
            .as_ref()
            .and_then(|v| v.as_cql_timestamp())
            .and_then(|ts| DateTime::from_timestamp_millis(ts.0));

        let retry_count: i32 = row.columns[5]
            .as_ref()
            .and_then(|v| v.as_int())
            .unwrap_or(0);

        let last_error: Option<String> = row.columns[6]
            .as_ref()
            .and_then(|v| v.as_text())
            .map(|s| s.to_string());

        let last_retry_at: Option<DateTime<Utc>> = row.columns[7]
            .as_ref()
            .and_then(|v| v.as_cql_timestamp())
            .and_then(|ts| DateTime::from_timestamp_millis(ts.0));

        Ok(FhirMessage {
            id,
            payload,
            status: MessageStatus::from(status),
            received_at,
            sent_at,
            retry_count,
            last_error,
            last_retry_at,
        })
    }

    /// Verifica a saúde da conexão
    pub async fn health_check(&self) -> Result<()> {
        self.session
            .query(format!("SELECT * FROM {}.fhir_messages LIMIT 1", self.keyspace), &[])
            .await
            .context("Health check falhou")?;
        Ok(())
    }
}
