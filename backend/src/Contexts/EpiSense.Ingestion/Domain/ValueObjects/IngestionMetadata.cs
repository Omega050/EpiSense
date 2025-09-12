using MongoDB.Bson.Serialization.Attributes;

namespace EpiSense.Ingestion.Domain.ValueObjects;

[BsonNoId]
public class IngestionMetadata
{
    [BsonElement("sourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;
    
    [BsonElement("sourceUrl")]
    public string? SourceUrl { get; set; }
    
    [BsonElement("receivedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("processedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAt { get; set; }
    
    [BsonElement("status")]
    public IngestionStatus Status { get; set; } = IngestionStatus.Received;
    
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }
    
    [BsonElement("retryCount")]
    public int RetryCount { get; set; } = 0;
    
    [BsonElement("lastRetryAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LastRetryAt { get; set; }
    
    [BsonElement("validationRules")]
    public List<string> ValidationRules { get; set; } = new();
    
    [BsonElement("transformationApplied")]
    public List<string> TransformationApplied { get; set; } = new();
    
    [BsonElement("checksum")]
    public string? Checksum { get; set; }
    
    [BsonElement("version")]
    public string Version { get; set; } = "1.0";
}

public enum IngestionStatus
{
    Received,
    Validating,
    ValidationFailed,
    Validated,
    Transforming,
    TransformationFailed,
    Transformed,
    Processing,
    Processed,
    Failed,
    Quarantine
}
