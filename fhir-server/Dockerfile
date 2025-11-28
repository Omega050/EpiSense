# Etapa 1: Builder - Compilação do projeto
FROM rust:1.82-slim AS builder

# Instalar dependências de build
RUN apt-get update && apt-get install -y \
    pkg-config \
    libssl-dev \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

# Criar diretório de trabalho
WORKDIR /usr/src/fhir-server

# Copiar manifests primeiro (para cache de dependências)
COPY Cargo.toml Cargo.lock ./

# Criar arquivos dummy para compilar apenas as dependências (cache layer)
RUN mkdir src && \
    echo "pub mod config; pub mod db; pub mod forwarder; pub mod metrics; pub mod models; pub mod retry_worker; pub mod routes;" > src/lib.rs && \
    echo "fn main() {}" > src/main.rs && \
    for module in config db forwarder metrics models retry_worker routes; do \
    echo "pub fn dummy() {}" > src/${module}.rs; \
    done

# Compilar dependências (esta camada será cacheada se Cargo.toml não mudar)
RUN cargo build --release && \
    rm -rf src

# Agora copiar o código fonte real
COPY src ./src

# Limpar artefatos do build dummy antes do build final
RUN rm -rf target/release/.fingerprint/fhir-server-* \
    target/release/deps/fhir_server* \
    target/release/deps/libfhir_server* \
    target/release/fhir-server* \
    target/release/libfhir-server*

# Build final em modo release
RUN cargo build --release

# Etapa 2: Runtime - Imagem final (menor e mais segura)
FROM debian:bookworm-slim

# Instalar apenas dependências de runtime necessárias
RUN apt-get update && apt-get install -y \
    ca-certificates \
    libssl3 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Criar usuário não-root para segurança
RUN useradd -m -u 1000 fhir && \
    mkdir -p /app && \
    chown -R fhir:fhir /app

WORKDIR /app

# Copiar apenas o binário compilado da fase de build
COPY --from=builder /usr/src/fhir-server/target/release/fhir-server /app/fhir-server

# Copiar script de entrypoint
COPY entrypoint.sh /app/entrypoint.sh

RUN sed -i 's/\r$//' /app/entrypoint.sh

# Garantir que o binário e o script sejam executáveis
RUN chmod +x /app/fhir-server /app/entrypoint.sh

# Trocar para usuário não-root
USER fhir

# Expor porta (ajustar conforme configuração)
EXPOSE 8081

# Variáveis de ambiente padrão (podem ser sobrescritas)
ENV RUST_LOG=info
ENV RUST_BACKTRACE=1

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8081/api/health || exit 1

# Usar o script de entrypoint
ENTRYPOINT ["/app/entrypoint.sh"]

# Comando de execução (pode ser sobrescrito)
CMD []
