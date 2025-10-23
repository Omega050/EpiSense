CREATE KEYSPACE IF NOT EXISTS episense 
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

USE episense;

CREATE TABLE IF NOT EXISTS hemogramas (
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

CREATE INDEX IF NOT EXISTS hemogramas_patient_id_idx ON hemogramas (patient_id);
CREATE INDEX IF NOT EXISTS hemogramas_sent_to_api_idx ON hemogramas (sent_to_api);
