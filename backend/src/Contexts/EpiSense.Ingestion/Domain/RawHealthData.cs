using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using EpiSense.Ingestion.Domain.ValueObjects;

namespace EpiSense.Ingestion.Domain;

[BsonCollection("raw_health_data")]
public class RawHealthData
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("fhirObservation")]
    public FhirObservationData? FhirObservation { get; set; }
    
    [BsonElement("rawJson")]
    public string RawJson { get; set; } = string.Empty;
    
    [BsonElement("ingestionMetadata")]
    public IngestionMetadata IngestionMetadata { get; set; } = new();
    
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
