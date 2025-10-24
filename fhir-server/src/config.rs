use serde::Deserialize;
use std::time::Duration;

/// Configuração principal da aplicação
#[derive(Debug, Clone, Deserialize)]
pub struct Config {
    #[serde(default = "default_host")]
    pub host: String,

    #[serde(default = "default_port")]
    pub port: u16,

    pub scylla: ScyllaConfig,
    pub backend: BackendConfig,
    pub retry_worker: RetryWorkerConfig,
    pub performance: PerformanceConfig,
}

#[derive(Debug, Clone, Deserialize)]
pub struct ScyllaConfig {
    pub nodes: String, // Comma-separated list
    pub keyspace: String,
    
    #[serde(default)]
    pub username: Option<String>,
    
    #[serde(default)]
    pub password: Option<String>,
}

impl ScyllaConfig {
    pub fn node_list(&self) -> Vec<String> {
        self.nodes
            .split(',')
            .map(|s| s.trim().to_string())
            .collect()
    }
}

#[derive(Debug, Clone, Deserialize)]
pub struct BackendConfig {
    pub url: String,
    
    #[serde(default = "default_backend_timeout_secs")]
    pub timeout_secs: u64,
    
    #[serde(default = "default_max_retries")]
    pub max_retries: u32,
    
    #[serde(default = "default_initial_backoff_ms")]
    pub initial_backoff_ms: u64,
    
    #[serde(default = "default_max_backoff_ms")]
    pub max_backoff_ms: u64,
}

impl BackendConfig {
    pub fn timeout(&self) -> Duration {
        Duration::from_secs(self.timeout_secs)
    }

    pub fn initial_backoff(&self) -> Duration {
        Duration::from_millis(self.initial_backoff_ms)
    }

    pub fn max_backoff(&self) -> Duration {
        Duration::from_millis(self.max_backoff_ms)
    }
}

#[derive(Debug, Clone, Deserialize)]
pub struct RetryWorkerConfig {
    #[serde(default = "default_retry_interval_secs")]
    pub interval_secs: u64,
    
    #[serde(default = "default_batch_size")]
    pub batch_size: i32,
}

impl RetryWorkerConfig {
    pub fn interval(&self) -> Duration {
        Duration::from_secs(self.interval_secs)
    }
}

#[derive(Debug, Clone, Deserialize)]
pub struct PerformanceConfig {
    #[serde(default = "default_http_workers")]
    pub workers: usize,
    
    #[serde(default = "default_max_connections")]
    pub max_connections: usize,
    
    #[serde(default = "default_keep_alive_secs")]
    pub keep_alive_secs: u64,
}

impl PerformanceConfig {
    pub fn keep_alive(&self) -> Duration {
        Duration::from_secs(self.keep_alive_secs)
    }
}

// Valores padrão
fn default_host() -> String {
    "0.0.0.0".to_string()
}

fn default_port() -> u16 {
    8080
}

fn default_backend_timeout_secs() -> u64 {
    30
}

fn default_max_retries() -> u32 {
    5
}

fn default_initial_backoff_ms() -> u64 {
    100
}

fn default_max_backoff_ms() -> u64 {
    60000
}

fn default_retry_interval_secs() -> u64 {
    60
}

fn default_batch_size() -> i32 {
    100
}

fn default_http_workers() -> usize {
    8
}

fn default_max_connections() -> usize {
    256
}

fn default_keep_alive_secs() -> u64 {
    75
}

impl Config {
    /// Carrega configuração a partir de variáveis de ambiente
    pub fn from_env() -> Result<Self, envy::Error> {
        dotenv::dotenv().ok();
        
        // Carrega as configurações aninhadas separadamente com seus prefixos
        let scylla = envy::prefixed("SCYLLA_").from_env::<ScyllaConfig>()?;
        let backend = envy::prefixed("BACKEND_").from_env::<BackendConfig>()?;
        let retry_worker = envy::prefixed("RETRY_WORKER_").from_env::<RetryWorkerConfig>()?;
        let performance = envy::prefixed("HTTP_").from_env::<PerformanceConfig>()?;
        
        // Carrega as configurações de nível superior
        let host = std::env::var("HOST").unwrap_or_else(|_| default_host());
        let port = std::env::var("PORT")
            .ok()
            .and_then(|s| s.parse().ok())
            .unwrap_or_else(default_port);
        
        Ok(Config {
            host,
            port,
            scylla,
            backend,
            retry_worker,
            performance,
        })
    }
}
