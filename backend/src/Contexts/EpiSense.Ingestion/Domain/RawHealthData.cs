using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EpiSense.Ingestion.Domain;

/// <summary>
/// Representa dados FHIR brutos armazenados durante a ingestão.
/// Mantém apenas o JSON original e metadados mínimos para rastreamento.
/// </summary>
[BsonCollection("raw_health_data")]
public class RawHealthData
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    /// <summary>
    /// JSON FHIR original recebido (sem processamento)
    /// </summary>
    [BsonElement("rawJson")]
    public string RawJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Sistema que enviou os dados (ex: "HAPI-FHIR", "Epic", "Cerner")
    /// </summary>
    [BsonElement("sourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp de quando os dados foram recebidos
    /// </summary>
    [BsonElement("receivedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Status do processamento dos dados
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public IngestionStatus Status { get; set; } = IngestionStatus.Received;
    
    /// <summary>
    /// Mensagem de erro (apenas se Status = Failed)
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// URL da requisição original (opcional, para auditoria)
    /// </summary>
    [BsonElement("sourceUrl")]
    public string? SourceUrl { get; set; }
}

/// <summary>
/// Estados possíveis dos dados durante a ingestão
/// </summary>
public enum IngestionStatus
{
    /// <summary>
    /// Dados recebidos e validados como JSON válido
    /// </summary>
    Received,
    
    /// <summary>
    /// Dados processados pelo módulo de análise
    /// </summary>
    Processed,
    
    /// <summary>
    /// Falha na validação ou processamento
    /// </summary>
    Failed
}

// Atributo para definir o nome da collection no MongoDB
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
