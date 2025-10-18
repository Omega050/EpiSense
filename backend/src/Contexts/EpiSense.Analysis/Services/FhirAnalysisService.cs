using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
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
    /// <param name="fhirJson">JSON FHIR da observação</param>
    /// <param name="rawDataId">ID do dado bruto no MongoDB (opcional)</param>
    /// <param name="receivedAt">Data de recebimento do dado (opcional)</param>
    public ObservationSummary AnalyzeObservation(string fhirJson, string? rawDataId = null, DateTime? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(fhirJson))
        {
            throw new ArgumentException("JSON FHIR não pode ser vazio", nameof(fhirJson));
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(fhirJson);
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
                RawDataId = rawDataId,
                DataColeta = GetEffectiveDateTime(fhirRoot) ?? receivedAt ?? DateTime.UtcNow,
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
            throw new ArgumentException($"Erro ao analisar JSON FHIR: {ex.Message}", nameof(fhirJson), ex);
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
    /// Obtém a data/hora efetiva da observação (sempre em UTC)
    /// </summary>
    private DateTime? GetEffectiveDateTime(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("effectiveDateTime", out var effectiveElement))
        {
            if (DateTime.TryParse(effectiveElement.GetString(), out var effectiveDate))
            {
                // Garantir que sempre retorna UTC
                return effectiveDate.Kind == DateTimeKind.Utc 
                    ? effectiveDate 
                    : effectiveDate.ToUniversalTime();
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
                case LoincCodes.PLAQUETAS:
                    summary.LabValues["plaquetas"] = value.Value;
                    break;
                case LoincCodes.LEUCOCITOS:
                    summary.LabValues["leucocitos"] = value.Value;
                    break;
                case LoincCodes.HEMATOCRITO:
                    summary.LabValues["hematocrito"] = value.Value;
                    break;
                case LoincCodes.HEMOGLOBINA:
                    summary.LabValues["hemoglobina"] = value.Value;
                    break;
                case LoincCodes.VCM:
                case LoincCodes.VCM_ALT:  // Aceita código alternativo 789-8
                    summary.LabValues["vcm"] = value.Value;
                    break;
                case LoincCodes.RDW:
                    summary.LabValues["rdw"] = value.Value;
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
    /// Aplica regras clínicas baseadas em evidências científicas para detectar dengue e anemia
    /// Referências: Guidelines OMS para dengue e critérios diagnósticos de anemia
    /// </summary>
    private void ApplyClinicalRules(ObservationSummary summary)
    {
        var flags = new List<string>();

        // === DETECÇÃO DE SINAIS INDIVIDUAIS - DENGUE ===
        
        // 1. Plaquetas - Trombocitopenia (< 100.000/mm³)
        bool hasTrimbocitopenia = false;
        if (summary.LabValues.TryGetValue("plaquetas", out var plaquetas))
        {
            if (plaquetas < ClinicalThresholds.DENGUE_PLAQUETAS_BAIXAS)
            {
                flags.Add(ClinicalFlags.TROMBOCITOPENIA);
                hasTrimbocitopenia = true;
            }
        }

        // 2. Leucócitos - Leucopenia
        bool hasLeucopenia = false;
        if (summary.LabValues.TryGetValue("leucocitos", out var leucocitos))
        {
            if (leucocitos < ClinicalThresholds.DENGUE_LEUCOPENIA_INTENSA)
            {
                flags.Add(ClinicalFlags.LEUCOPENIA_INTENSA);
                hasLeucopenia = true;
            }
            else if (leucocitos < ClinicalThresholds.DENGUE_LEUCOPENIA_MODERADA)
            {
                flags.Add(ClinicalFlags.LEUCOPENIA_MODERADA);
                hasLeucopenia = true;
            }
        }

        // 3. Hematócrito - Hemoconcentração (sinal de alarme para dengue grave)
        bool hasHemoconcentracao = false;
        if (summary.LabValues.TryGetValue("hematocrito", out var hematocrito))
        {
            // Critérios: > 40% (mulheres) ou > 45% (homens)
            // Nota: Sem informação de sexo, usa limite mais conservador
            if (hematocrito > ClinicalThresholds.HEMATOCRITO_MULHER_ALTO)
            {
                flags.Add(ClinicalFlags.HEMOCONCENTRACAO);
                hasHemoconcentracao = true;
            }
        }

        // === PADRÃO COMPOSTO - DENGUE ===
        // Dengue = Trombocitopenia + Leucopenia + Hemoconcentração
        if (hasTrimbocitopenia && hasLeucopenia && hasHemoconcentracao)
        {
            flags.Add(ClinicalFlags.DENGUE);
        }

        // === DETECÇÃO DE SINAIS INDIVIDUAIS - ANEMIA ===
        
        // 1. Hemoglobina Baixa
        bool hasHemoglobinaBaixa = false;
        if (summary.LabValues.TryGetValue("hemoglobina", out var hemoglobina))
        {
            // Critérios: < 12 g/dL (mulheres), < 13.6 g/dL (homens)
            // Usa limite mais sensível (mulheres) na ausência de informação de sexo
            if (hemoglobina < ClinicalThresholds.ANEMIA_HEMOGLOBINA_MULHER)
            {
                flags.Add(ClinicalFlags.HEMOGLOBINA_BAIXA);
                hasHemoglobinaBaixa = true;
            }
        }

        // 2. VCM - Microcitose (< 80 fL)
        bool hasMicrocitose = false;
        if (summary.LabValues.TryGetValue("vcm", out var vcm))
        {
            if (vcm < ClinicalThresholds.ANEMIA_VCM_MICROCITOSE)
            {
                flags.Add(ClinicalFlags.MICROCITOSE);
                hasMicrocitose = true;
            }
        }

        // 3. RDW - Anisocitose (variação no tamanho das hemácias)
        bool hasAnisocitose = false;
        if (summary.LabValues.TryGetValue("rdw", out var rdw))
        {
            // RDW normal geralmente é 11.5-14.5%
            // Valores acima de 14.5% indicam anisocitose
            if (rdw > 14.5m)
            {
                flags.Add(ClinicalFlags.ANISOCITOSE);
                hasAnisocitose = true;
            }
        }

        // === PADRÃO COMPOSTO - ANEMIA ===
        // Anemia = Hemoglobina baixa + Microcitose + Anisocitose
        if (hasHemoglobinaBaixa && hasMicrocitose && hasAnisocitose)
        {
            flags.Add(ClinicalFlags.ANEMIA);
        }

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