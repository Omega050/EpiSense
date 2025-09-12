using MongoDB.Bson.Serialization.Attributes;

namespace EpiSense.Ingestion.Domain.ValueObjects;

[BsonNoId]
public class FhirCodeableConcept
{
    [BsonElement("coding")]
    public List<FhirCoding> Coding { get; set; } = new();
    
    [BsonElement("text")]
    public string? Text { get; set; }
}

[BsonNoId]
public class FhirCoding
{
    [BsonElement("system")]
    public string? System { get; set; }
    
    [BsonElement("version")]
    public string? Version { get; set; }
    
    [BsonElement("code")]
    public string? Code { get; set; }
    
    [BsonElement("display")]
    public string? Display { get; set; }
    
    [BsonElement("userSelected")]
    public bool? UserSelected { get; set; }
}

[BsonNoId]
public class FhirReference
{
    [BsonElement("reference")]
    public string? Reference { get; set; }
    
    [BsonElement("type")]
    public string? Type { get; set; }
    
    [BsonElement("identifier")]
    public FhirIdentifier? Identifier { get; set; }
    
    [BsonElement("display")]
    public string? Display { get; set; }
}

[BsonNoId]
public class FhirIdentifier
{
    [BsonElement("use")]
    public string? Use { get; set; }
    
    [BsonElement("type")]
    public FhirCodeableConcept? Type { get; set; }
    
    [BsonElement("system")]
    public string? System { get; set; }
    
    [BsonElement("value")]
    public string? Value { get; set; }
    
    [BsonElement("period")]
    public FhirPeriod? Period { get; set; }
    
    [BsonElement("assigner")]
    public FhirReference? Assigner { get; set; }
}

[BsonNoId]
public class FhirPeriod
{
    [BsonElement("start")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Start { get; set; }
    
    [BsonElement("end")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? End { get; set; }
}

[BsonNoId]
public class FhirQuantity
{
    [BsonElement("value")]
    public decimal? Value { get; set; }
    
    [BsonElement("comparator")]
    public string? Comparator { get; set; }
    
    [BsonElement("unit")]
    public string? Unit { get; set; }
    
    [BsonElement("system")]
    public string? System { get; set; }
    
    [BsonElement("code")]
    public string? Code { get; set; }
}

[BsonNoId]
public class FhirRange
{
    [BsonElement("low")]
    public FhirQuantity? Low { get; set; }
    
    [BsonElement("high")]
    public FhirQuantity? High { get; set; }
}

[BsonNoId]
public class FhirAnnotation
{
    [BsonElement("authorReference")]
    public FhirReference? AuthorReference { get; set; }
    
    [BsonElement("authorString")]
    public string? AuthorString { get; set; }
    
    [BsonElement("time")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Time { get; set; }
    
    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;
}

[BsonNoId]
public class FhirObservationReferenceRange
{
    [BsonElement("low")]
    public FhirQuantity? Low { get; set; }
    
    [BsonElement("high")]
    public FhirQuantity? High { get; set; }
    
    [BsonElement("type")]
    public FhirCodeableConcept? Type { get; set; }
    
    [BsonElement("appliesTo")]
    public List<FhirCodeableConcept> AppliesTo { get; set; } = new();
    
    [BsonElement("age")]
    public FhirRange? Age { get; set; }
    
    [BsonElement("text")]
    public string? Text { get; set; }
}

[BsonNoId]
public class FhirObservationComponent
{
    [BsonElement("code")]
    public FhirCodeableConcept Code { get; set; } = new();
    
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
    
    [BsonElement("referenceRange")]
    public List<FhirObservationReferenceRange> ReferenceRange { get; set; } = new();
}
