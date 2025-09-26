using EpiSense.Ingestion.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EpiSense.Api.DTOs;

public class FhirObservationRequest
{
    public string ResourceType { get; set; } = "Observation";
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<FhirCodeableConcept> Category { get; set; } = new();
    public FhirCodeableConcept Code { get; set; } = new();
    public FhirReference Subject { get; set; } = new();
    public FhirReference? Encounter { get; set; }
    public DateTime? EffectiveDateTime { get; set; }
    public FhirPeriod? EffectivePeriod { get; set; }
    public DateTime? Issued { get; set; }
    public List<FhirReference> Performer { get; set; } = new();
    public FhirQuantity? ValueQuantity { get; set; }
    public FhirCodeableConcept? ValueCodeableConcept { get; set; }
    public string? ValueString { get; set; }
    public bool? ValueBoolean { get; set; }
    public int? ValueInteger { get; set; }
    public FhirRange? ValueRange { get; set; }
    public FhirCodeableConcept? DataAbsentReason { get; set; }
    public List<FhirCodeableConcept> Interpretation { get; set; } = new();
    public List<FhirAnnotation> Note { get; set; } = new();
    public FhirCodeableConcept? BodySite { get; set; }
    public FhirCodeableConcept? Method { get; set; }
    public FhirReference? Specimen { get; set; }
    public FhirReference? Device { get; set; }
    public List<FhirObservationReferenceRange> ReferenceRange { get; set; } = new();
    public List<FhirObservationComponent> Component { get; set; } = new();

    /// <summary>
    /// Converte o objeto para JSON string (usado para compatibilidade com sistema atual)
    /// </summary>
    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }
}