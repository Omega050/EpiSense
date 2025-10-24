-- ============================================
-- ScyllaDB Unified Schema Initialization
-- EpiSense Project
-- ============================================

-- ============================================
-- FHIR Server Keyspace & Tables
-- ============================================

-- Keyspace para armazenar mensagens FHIR (fhir-server)
CREATE KEYSPACE IF NOT EXISTS fhir_server
WITH replication = {
    'class': 'SimpleStrategy',
    'replication_factor': 1
};

-- Tabela principal para mensagens FHIR
-- Otimizada para consultas por status e timestamp
CREATE TABLE IF NOT EXISTS fhir_server.fhir_messages (
    id uuid PRIMARY KEY,
    payload text,
    status text,
    received_at timestamp,
    sent_at timestamp,
    retry_count int,
    last_error text,
    last_retry_at timestamp
);

-- Índice secundário para consultas por status (para retry worker)
CREATE INDEX IF NOT EXISTS idx_status ON fhir_server.fhir_messages (status);

-- Índice secundário para consultas por timestamp (para ordenação e cleanup)
CREATE INDEX IF NOT EXISTS idx_received_at ON fhir_server.fhir_messages (received_at);

-- Tabela para tracking de métricas (opcional, para analytics)
CREATE TABLE IF NOT EXISTS fhir_server.metrics (
    date date,
    hour int,
    metric_type text,
    count counter,
    PRIMARY KEY ((date, hour), metric_type)
);


-- ============================================
-- FHIR Generator Keyspace & Tables
-- ============================================

-- Keyspace para armazenar hemogramas gerados (fhir-generator)
CREATE KEYSPACE IF NOT EXISTS fhir_generator 
WITH replication = {
    'class': 'SimpleStrategy',
    'replication_factor': 1
};

-- Tabela de hemogramas com dados FHIR
CREATE TABLE IF NOT EXISTS fhir_generator.hemogramas (
    id UUID PRIMARY KEY,
    patient_id TEXT,
    patient_name TEXT,
    collection_date TIMESTAMP,
    fhir_bundle_json TEXT,
    sent_to_api BOOLEAN,
    sent_at TIMESTAMP,
    api_response_status INT,
    created_at TIMESTAMP,
    red_blood_cells DOUBLE,
    hemoglobin DOUBLE,
    hematocrit DOUBLE,
    mcv DOUBLE,
    mch DOUBLE,
    mchc DOUBLE,
    rdw DOUBLE,
    white_blood_cells DOUBLE,
    neutrophils DOUBLE,
    lymphocytes DOUBLE,
    monocytes DOUBLE,
    eosinophils DOUBLE,
    basophils DOUBLE,
    platelets DOUBLE,
    mpv DOUBLE
);

-- Índices para otimização de consultas
CREATE INDEX IF NOT EXISTS hemogramas_patient_id_idx ON fhir_generator.hemogramas (patient_id);
CREATE INDEX IF NOT EXISTS hemogramas_sent_to_api_idx ON fhir_generator.hemogramas (sent_to_api);
