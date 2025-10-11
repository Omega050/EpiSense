using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
using EpiSense.Ingestion.Domain;
using System.Text.Json;

namespace EpiSense.Analysis.Services;

/// <summary>
/// Serviço de análise que processa observações FHIR e gera resumos clínicos
/// </summary>
public class FhirAnalysisService
{
    /// <summary>
    /// Analisa uma observação FHIR e gera um resumo clínico
    /// </summary>
    public ObservationSummary AnalyzeObservation(RawHealthData rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData.RawJson))
        {
            throw new ArgumentException("JSON FHIR não encontrado nos dados brutos", nameof(rawData));
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(rawData.RawJson);
            var fhirRoot = jsonDoc.RootElement;

            // Valida se é um recurso FHIR Observation
            if (!fhirRoot.TryGetProperty("resourceType", out var resourceType) || 
                resourceType.GetString() != "Observation")
            {
                throw new ArgumentException("Dados FHIR não são do tipo Observation");
            }

            var summary = new ObservationSummary
            {
                ObservationId = GetObservationId(fhirRoot),
                RawDataId = rawData.Id,
                DataColeta = GetEffectiveDateTime(fhirRoot) ?? rawData.ReceivedAt,
                ProcessedAt = DateTime.UtcNow
            };

            // Extrair valores laboratoriais dos componentes
            ExtractLabValues(fhirRoot, summary);
            
            // Aplicar regras clínicas para gerar flags
            ApplyClinicalRules(summary);
            
            // Tentar extrair código do município (se disponível nos dados)
            ExtractMunicipalityCode(fhirRoot, summary);

            return summary;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Erro ao analisar JSON FHIR: {ex.Message}", nameof(rawData), ex);
        }
    }

    /// <summary>
    /// Obtém o ID da observação FHIR
    /// </summary>
    private string GetObservationId(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("id", out var idElement))
        {
            return $"Observation/{idElement.GetString()}";
        }
        return $"Observation/{Guid.NewGuid()}";
    }

    /// <summary>
    /// Obtém a data/hora efetiva da observação
    /// </summary>
    private DateTime? GetEffectiveDateTime(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("effectiveDateTime", out var effectiveElement))
        {
            if (DateTime.TryParse(effectiveElement.GetString(), out var effectiveDate))
            {
                return effectiveDate;
            }
        }
        return null;
    }

    /// <summary>
    /// Extrai valores laboratoriais dos componentes FHIR
    /// </summary>
    private void ExtractLabValues(JsonElement fhirRoot, ObservationSummary summary)
    {
        if (!fhirRoot.TryGetProperty("component", out var componentsElement) || 
            componentsElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (var component in componentsElement.EnumerateArray())
        {
            var code = ExtractComponentCode(component);
            var value = ExtractComponentValue(component);

            if (string.IsNullOrEmpty(code) || !value.HasValue)
                continue;

            switch (code)
            {
                case LoincCodes.LEUCOCITOS:
                    summary.LabValues["leucocitos"] = value.Value;
                    break;
                case LoincCodes.PLAQUETAS:
                    summary.LabValues["plaquetas"] = value.Value;
                    break;
                case LoincCodes.HEMOGLOBINA:
                    summary.LabValues["hemoglobina"] = value.Value;
                    break;
                case LoincCodes.HEMATOCRITO:
                    summary.LabValues["hematocrito"] = value.Value;
                    break;
            }
        }

        // Também verifica se há um valor principal na observação
        var mainValue = ExtractMainValue(fhirRoot);
        var mainCode = ExtractMainCode(fhirRoot);
        
        if (mainValue.HasValue && !string.IsNullOrEmpty(mainCode))
        {
            summary.LabValues[mainCode] = mainValue.Value;
        }
    }

    /// <summary>
    /// Extrai o código de um componente FHIR
    /// </summary>
    private string? ExtractComponentCode(JsonElement component)
    {
        if (component.TryGetProperty("code", out var codeElement) &&
            codeElement.TryGetProperty("coding", out var codingElement) &&
            codingElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var coding in codingElement.EnumerateArray())
            {
                if (coding.TryGetProperty("code", out var codeValue))
                {
                    return codeValue.GetString();
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Extrai o valor numérico de um componente FHIR
    /// </summary>
    private decimal? ExtractComponentValue(JsonElement component)
    {
        if (component.TryGetProperty("valueQuantity", out var valueElement) &&
            valueElement.TryGetProperty("value", out var valueNumber) &&
            valueNumber.ValueKind == JsonValueKind.Number)
        {
            return valueNumber.GetDecimal();
        }
        return null;
    }

    /// <summary>
    /// Extrai o valor principal da observação FHIR
    /// </summary>
    private decimal? ExtractMainValue(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("valueQuantity", out var valueElement) &&
            valueElement.TryGetProperty("value", out var valueNumber) &&
            valueNumber.ValueKind == JsonValueKind.Number)
        {
            return valueNumber.GetDecimal();
        }
        return null;
    }

    /// <summary>
    /// Extrai o código principal da observação FHIR
    /// </summary>
    private string? ExtractMainCode(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("code", out var codeElement) &&
            codeElement.TryGetProperty("coding", out var codingElement) &&
            codingElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var coding in codingElement.EnumerateArray())
            {
                if (coding.TryGetProperty("code", out var codeValue))
                {
                    return codeValue.GetString();
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Aplica regras clínicas para detectar padrões anômalos
    /// </summary>
    private void ApplyClinicalRules(ObservationSummary summary)
    {
        var flags = new List<string>();

        // Análise de leucócitos
        if (summary.LabValues.TryGetValue("leucocitos", out var leucocitos))
        {
            if (leucocitos < 4000)
                flags.Add(ClinicalFlags.LEUCOPENIA);
            else if (leucocitos > 11000)
                flags.Add(ClinicalFlags.LEUCOCITOSE);
        }

        // Análise de plaquetas
        if (summary.LabValues.TryGetValue("plaquetas", out var plaquetas))
        {
            if (plaquetas < 150000)
            {
                flags.Add(ClinicalFlags.TROMBOCITOPENIA);
                
                // Padrão Dengue: trombocitopenia severa
                if (plaquetas < 100000)
                    flags.Add(ClinicalFlags.PADRAO_DENGUE);
            }
            else if (plaquetas > 450000)
            {
                flags.Add(ClinicalFlags.TROMBOCITOSE);
            }
        }

        // Análise de hemoglobina (usando valores genéricos)
        if (summary.LabValues.TryGetValue("hemoglobina", out var hemoglobina))
        {
            if (hemoglobina < 12.0m) // Limite genérico para anemia
                flags.Add(ClinicalFlags.ANEMIA);
            else if (hemoglobina > 16.5m) // Limite genérico para policitemia
                flags.Add(ClinicalFlags.POLICITEMIA);
        }

        // Padrões combinados
        if (flags.Contains(ClinicalFlags.LEUCOPENIA) && flags.Contains(ClinicalFlags.TROMBOCITOPENIA))
            flags.Add(ClinicalFlags.PADRAO_VIRAL);

        if (flags.Contains(ClinicalFlags.LEUCOCITOSE))
            flags.Add(ClinicalFlags.PADRAO_BACTERIANO);

        summary.Flags = flags;
    }

    /// <summary>
    /// Tenta extrair código do município dos dados FHIR
    /// </summary>
    private void ExtractMunicipalityCode(JsonElement fhirRoot, ObservationSummary summary)
    {
        // Por enquanto, deixamos como null
        // Em uma implementação real, isso viria do subject/patient ou performer/organization
        summary.CodigoMunicipioIBGE = null;
        
        // TODO: Implementar extração baseada em:
        // - fhirRoot.subject?.reference (dados do paciente)
        // - fhirRoot.performer (organização responsável)
        
        // Exemplo de como acessaria:
        // if (fhirRoot.TryGetProperty("subject", out var subjectElement) &&
        //     subjectElement.TryGetProperty("reference", out var referenceElement))
        // {
        //     var reference = referenceElement.GetString();
        //     // Buscar dados do paciente e extrair código do município
        // }
    }
}