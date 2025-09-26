// Script de inicialização do MongoDB para EpiSense
// Este script cria o database e collections necessárias

db = db.getSiblingDB('episense_dev');

// Cria as collections principais
db.createCollection('raw_health_data', {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["rawJson", "ingestionMetadata", "createdAt"],
      properties: {
        rawJson: {
          bsonType: "string",
          description: "JSON bruto da observação FHIR"
        },
        ingestionMetadata: {
          bsonType: "object",
          required: ["sourceSystem", "receivedAt", "status"],
          properties: {
            sourceSystem: {
              bsonType: "string",
              description: "Sistema de origem dos dados"
            },
            status: {
              enum: ["Received", "Validating", "ValidationFailed", "Validated", "Transforming", "TransformationFailed", "Transformed", "Processing", "Processed", "Failed", "Quarantine"],
              description: "Status do processamento"
            }
          }
        }
      }
    }
  }
});

db.createCollection('analytics_aggregates');
db.createCollection('alerts');

// Cria índices para performance
db.raw_health_data.createIndex({ "ingestionMetadata.status": 1 });
db.raw_health_data.createIndex({ "ingestionMetadata.receivedAt": 1 });
db.raw_health_data.createIndex({ "createdAt": 1 });
db.raw_health_data.createIndex({ "fhirObservation.fhirId": 1 }, { sparse: true });

print("✅ Database 'episense_dev' inicializado com sucesso!");
print("📊 Collections criadas: raw_health_data, analytics_aggregates, alerts");
print("🔍 Índices criados para otimização de consultas");