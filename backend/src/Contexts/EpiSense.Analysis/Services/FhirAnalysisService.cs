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
    /// <param name="fhirJson">JSON FHIR da observação ou Bundle contendo observações</param>
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

            // Verifica o tipo de recurso FHIR
            if (!fhirRoot.TryGetProperty("resourceType", out var resourceType))
            {
                throw new ArgumentException("JSON FHIR não possui propriedade 'resourceType'");
            }

            var resourceTypeStr = resourceType.GetString();
            JsonElement? bundleRoot = null;
            
            // Se for um Bundle, extrai a primeira Observation e guarda referência ao Bundle
            if (resourceTypeStr == "Bundle")
            {
                bundleRoot = fhirRoot;
                fhirRoot = ExtractObservationFromBundle(fhirRoot);
            }
            else if (resourceTypeStr != "Observation")
            {
                throw new ArgumentException($"Dados FHIR não são do tipo Observation ou Bundle. Tipo recebido: {resourceTypeStr}");
            }

            var summary = new ObservationSummary
            {
                ObservationId = GetObservationId(fhirRoot),
                DataColeta = GetEffectiveDateTime(fhirRoot) ?? receivedAt ?? DateTime.UtcNow
            };

            // Extrair valores laboratoriais dos componentes
            ExtractLabValues(fhirRoot, summary);
            
            // Aplicar regras clínicas para gerar flags
            ApplyClinicalRules(summary);
            
            // Extrair código do município do Patient (se Bundle estiver disponível)
            if (bundleRoot.HasValue)
            {
                ExtractMunicipalityCodeFromBundle(bundleRoot.Value, summary);
            }
            else
            {
                ExtractMunicipalityCode(fhirRoot, summary);
            }

            return summary;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Erro ao analisar JSON FHIR: {ex.Message}", nameof(fhirJson), ex);
        }
    }

    /// <summary>
    /// Extrai a primeira Observation de um Bundle FHIR
    /// </summary>
    private JsonElement ExtractObservationFromBundle(JsonElement bundleRoot)
    {
        if (!bundleRoot.TryGetProperty("entry", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Bundle FHIR não contém array 'entry'");
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.TryGetProperty("resource", out var resource) &&
                resource.TryGetProperty("resourceType", out var resourceType) &&
                resourceType.GetString() == "Observation")
            {
                return resource;
            }
        }

        throw new ArgumentException("Bundle FHIR não contém nenhum recurso do tipo Observation");
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
                case LoincCodes.LEUCOCITOS:
                    summary.LabValues["leucocitos"] = value.Value;
                    break;
                case LoincCodes.NEUTROFILOS:
                case LoincCodes.NEUTROFILOS_ALT:  // Aceita código alternativo
                    summary.LabValues["neutrofilos"] = value.Value;
                    break;
                case LoincCodes.BASTONETES:
                case LoincCodes.BASTONETES_ALT:  // Aceita código alternativo 711-2
                    summary.LabValues["bastonetes"] = value.Value;
                    break;
                case LoincCodes.BASTONETES_PCT:
                    summary.LabValues["bastonetes_pct"] = value.Value;
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
            var value = valueNumber.GetDecimal();
            
            // Verifica a unidade para fazer conversão se necessário
            if (valueElement.TryGetProperty("code", out var unitCode))
            {
                var unit = unitCode.GetString();
                
                // Se a unidade for 10*3/uL ou x10*3/uL, multiplica por 1000
                // para converter para células/μL
                if (unit == "10*3/uL" || unit == "x10*3/uL")
                {
                    value *= 1000m;
                }
            }
            
            return value;
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
    /// Aplica regras clínicas baseadas em evidências científicas para detectar Síndrome de Infecção Bacteriana (SIB)
    /// Referências: Critérios hematológicos para infecção bacteriana em adultos
    /// </summary>
    private void ApplyClinicalRules(ObservationSummary summary)
    {
        var flags = new List<string>();

        // === DETECÇÃO DE SINAIS INDIVIDUAIS - INFECÇÃO BACTERIANA ===
        
        // 1. Leucócitos Totais - Leucocitose (> 11.000/μL)
        bool hasLeucocitose = false;
        if (summary.LabValues.TryGetValue("leucocitos", out var leucocitos))
        {
            if (leucocitos > ClinicalThresholds.LEUCOCITOSE)
            {
                flags.Add(ClinicalFlags.Laboratory.LEUCOCITOSE);
                hasLeucocitose = true;
            }
        }

        // 2. Neutrófilos Absolutos - Neutrofilia (> 7.500/μL)
        bool hasNeutrofilia = false;
        if (summary.LabValues.TryGetValue("neutrofilos", out var neutrofilos))
        {
            if (neutrofilos > ClinicalThresholds.NEUTROFILIA)
            {
                flags.Add(ClinicalFlags.Laboratory.NEUTROFILIA);
                hasNeutrofilia = true;
            }
        }

        // 3. Bastonetes - Desvio à Esquerda (> 500/μL ou > 10%)
        bool hasDesvioEsquerda = false;
        
        // Verifica valor absoluto de bastonetes
        if (summary.LabValues.TryGetValue("bastonetes", out var bastonetes))
        {
            if (bastonetes > ClinicalThresholds.DESVIO_ESQUERDA)
            {
                flags.Add(ClinicalFlags.Laboratory.DESVIO_ESQUERDA);
                hasDesvioEsquerda = true;
            }
        }
        
        // Verifica percentual de bastonetes (se valor absoluto não foi detectado)
        if (!hasDesvioEsquerda && summary.LabValues.TryGetValue("bastonetes_pct", out var bastonetesPct))
        {
            if (bastonetesPct > ClinicalThresholds.DESVIO_ESQUERDA_PCT)
            {
                flags.Add(ClinicalFlags.Laboratory.DESVIO_ESQUERDA);
                hasDesvioEsquerda = true;
            }
        }

        // === PADRÃO COMPOSTO - SUSPEITA DE SIB (Flag Clínica) ===
        // SIB Suspeita = Leucocitose (> 11.000/μL) + Neutrofilia (> 7.500/μL)
        if (hasLeucocitose && hasNeutrofilia)
        {
            flags.Add(ClinicalFlags.Clinical.SIB_SUSPEITA);
        }

        // === PADRÃO COMPOSTO - SIB GRAVE (Flag Clínica) ===
        // SIB Grave = Neutrofilia (> 7.500/μL) + Desvio à Esquerda (Bastonetes > 500/μL ou > 10%)
        // Este padrão indica infecção mais grave, independente da contagem total de leucócitos
        if (hasNeutrofilia && hasDesvioEsquerda)
        {
            flags.Add(ClinicalFlags.Clinical.SIB_GRAVE);
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
    
    /// <summary>
    /// Extrai o código do município IBGE do recurso Patient no Bundle
    /// Mapeia cidade brasileira para código IBGE de 7 dígitos
    /// </summary>
    private void ExtractMunicipalityCodeFromBundle(JsonElement bundleRoot, ObservationSummary summary)
    {
        if (!bundleRoot.TryGetProperty("entry", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        // Procura o recurso Patient no Bundle
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource) ||
                !resource.TryGetProperty("resourceType", out var resourceType) ||
                resourceType.GetString() != "Patient")
            {
                continue;
            }

            // Extrai endereço do paciente
            if (!resource.TryGetProperty("address", out var addresses) ||
                addresses.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var address in addresses.EnumerateArray())
            {
                // Obtém cidade e estado
                string? city = null;
                string? state = null;

                if (address.TryGetProperty("city", out var cityElement))
                {
                    city = cityElement.GetString();
                }

                if (address.TryGetProperty("state", out var stateElement))
                {
                    state = stateElement.GetString();
                }

                // Se encontrou cidade e estado, mapeia para código IBGE
                if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state))
                {
                    summary.CodigoMunicipioIBGE = MapCityToIBGECode(city, state);
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// Mapeia nome da cidade e estado para código IBGE
    /// Referência: https://www.ibge.gov.br/explica/codigos-dos-municipios.php
    /// </summary>
    private string? MapCityToIBGECode(string city, string state)
    {
        // Remove acentos e normaliza para comparação
        var normalizedCity = city.ToUpperInvariant().Trim();
        var normalizedState = state.ToUpperInvariant().Trim();

        // Mapeamento de algumas cidades principais (expandir conforme necessário)
        // Formato: "Cidade|Estado" -> Código IBGE 7 dígitos
        var cityMap = new Dictionary<string, string>
        {
            // Goiás
            ["TRINDADE|GO"] = "5221403",
            ["GOIANIA|GO"] = "5208707",
            ["APARECIDA DE GOIANIA|GO"] = "5201405",
            ["ANAPOLIS|GO"] = "5201108",
            
            // Capitais principais
            ["SAO PAULO|SP"] = "3550308",
            ["RIO DE JANEIRO|RJ"] = "3304557",
            ["BRASILIA|DF"] = "5300108",
            ["BELO HORIZONTE|MG"] = "3106200",
            ["SALVADOR|BA"] = "2927408",
            ["FORTALEZA|CE"] = "2304400",
            ["RECIFE|PE"] = "2611606",
            ["CURITIBA|PR"] = "4106902",
            ["PORTO ALEGRE|RS"] = "4314902",
            ["MANAUS|AM"] = "1302603"
        };

        var key = $"{normalizedCity}|{normalizedState}";
        
        if (cityMap.TryGetValue(key, out var ibgeCode))
        {
            return ibgeCode;
        }

        // TODO: Implementar busca em base completa de municípios IBGE
        // Por enquanto, retorna null para cidades não mapeadas
        return null;
    }
}