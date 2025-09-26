using EpiSense.Api.DTOs;
using EpiSense.Ingestion.Domain;
using System.Text.Json;

namespace EpiSense.Api.Extensions;

public static class FhirExtensions
{
    /// <summary>
    /// Converte um objeto FhirObservationRequest para FhirObservationData
    /// </summary>
    public static FhirObservationData ToFhirObservationData(this FhirObservationRequest request)
    {
        return new FhirObservationData
        {
            Id = Guid.NewGuid().ToString(),
            FhirId = request.Id,
            ResourceType = request.ResourceType,
            Status = request.Status,
            Category = request.Category,
            Code = request.Code,
            Subject = request.Subject,
            Encounter = request.Encounter,
            EffectiveDateTime = request.EffectiveDateTime,
            EffectivePeriod = request.EffectivePeriod,
            Issued = request.Issued,
            Performer = request.Performer,
            ValueQuantity = request.ValueQuantity,
            ValueCodeableConcept = request.ValueCodeableConcept,
            ValueString = request.ValueString,
            ValueBoolean = request.ValueBoolean,
            ValueInteger = request.ValueInteger,
            ValueRange = request.ValueRange,
            DataAbsentReason = request.DataAbsentReason,
            Interpretation = request.Interpretation,
            Note = request.Note,
            BodySite = request.BodySite,
            Method = request.Method,
            Specimen = request.Specimen,
            Device = request.Device,
            ReferenceRange = request.ReferenceRange,
            Component = request.Component
        };
    }

    /// <summary>
    /// Converte um JSON string para FhirObservationRequest
    /// </summary>
    public static FhirObservationRequest? FromJsonString(string jsonString)
    {
        try
        {
            return JsonSerializer.Deserialize<FhirObservationRequest>(jsonString, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Valida se um FhirObservationRequest contém os dados mínimos necessários
    /// </summary>
    public static bool IsValid(this FhirObservationRequest request, out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        if (string.IsNullOrEmpty(request.ResourceType) || request.ResourceType != "Observation")
        {
            validationErrors.Add("ResourceType must be 'Observation'");
        }

        if (string.IsNullOrEmpty(request.Id))
        {
            validationErrors.Add("Id is required");
        }

        if (string.IsNullOrEmpty(request.Status))
        {
            validationErrors.Add("Status is required");
        }

        if (request.Subject == null || string.IsNullOrEmpty(request.Subject.Reference))
        {
            validationErrors.Add("Subject reference is required");
        }

        if (request.Code == null || !request.Code.Coding.Any())
        {
            validationErrors.Add("Code with at least one coding is required");
        }

        return !validationErrors.Any();
    }
}