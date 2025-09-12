using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using EpiSense.Ingestion.Domain.ValueObjects;

namespace EpiSense.Ingestion.Domain;

public class FhirObservationData
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("fhirId")]
    public string FhirId { get; set; } = string.Empty;
    
    [BsonElement("resourceType")]
    public string ResourceType { get; set; } = "Observation";
    
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;
    
    [BsonElement("category")]
    public List<FhirCodeableConcept> Category { get; set; } = new();
    
    [BsonElement("code")]
    public FhirCodeableConcept Code { get; set; } = new();
    
    [BsonElement("subject")]
    public FhirReference Subject { get; set; } = new();
    
    [BsonElement("encounter")]
    public FhirReference? Encounter { get; set; }
    
    [BsonElement("effectiveDateTime")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? EffectiveDateTime { get; set; }
    
    [BsonElement("effectivePeriod")]
    public FhirPeriod? EffectivePeriod { get; set; }
    
    [BsonElement("issued")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Issued { get; set; }
    
    [BsonElement("performer")]
    public List<FhirReference> Performer { get; set; } = new();
    
    [BsonElement("valueQuantity")]
    public FhirQuantity? ValueQuantity { get; set; }
    
    [BsonElement("valueCodeableConcept")]
    public FhirCodeableConcept? ValueCodeableConcept { get; set; }
    
    [BsonElement("valueString")]
    public string? ValueString { get; set; }
    
    [BsonElement("valueBoolean")]
    public bool? ValueBoolean { get; set; }
    
    [BsonElement("valueInteger")]
    public int? ValueInteger { get; set; }
    
    [BsonElement("valueRange")]
    public FhirRange? ValueRange { get; set; }
    
    [BsonElement("dataAbsentReason")]
    public FhirCodeableConcept? DataAbsentReason { get; set; }
    
    [BsonElement("interpretation")]
    public List<FhirCodeableConcept> Interpretation { get; set; } = new();
    
    [BsonElement("note")]
    public List<FhirAnnotation> Note { get; set; } = new();
    
    [BsonElement("bodySite")]
    public FhirCodeableConcept? BodySite { get; set; }
    
    [BsonElement("method")]
    public FhirCodeableConcept? Method { get; set; }
    
    [BsonElement("specimen")]
    public FhirReference? Specimen { get; set; }
    
    [BsonElement("device")]
    public FhirReference? Device { get; set; }
    
    [BsonElement("referenceRange")]
    public List<FhirObservationReferenceRange> ReferenceRange { get; set; } = new();
    
    [BsonElement("component")]
    public List<FhirObservationComponent> Component { get; set; } = new();
}
