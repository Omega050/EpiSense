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
    /// <remarks>
    /// Se for um Bundle, processa TODAS as Observations e consolida os valores laboratoriais
    /// em um único ObservationSummary. Isso garante detecção correta de flags clínicas que
    /// requerem múltiplos valores (ex: SIB_SUSPEITA = Leucocitose + Neutrofilia).
    /// </remarks>
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
            
            // Processa Bundle ou Observation única
            if (resourceTypeStr == "Bundle")
            {
                return AnalyzeBundle(fhirRoot, rawDataId, receivedAt);
            }
            else if (resourceTypeStr == "Observation")
            {
                return AnalyzeSingleObservation(fhirRoot, rawDataId, receivedAt);
            }
            else
            {
                throw new ArgumentException($"Dados FHIR não são do tipo Observation ou Bundle. Tipo recebido: {resourceTypeStr}");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Erro ao analisar JSON FHIR: {ex.Message}", nameof(fhirJson), ex);
        }
    }

    /// <summary>
    /// Processa um Bundle FHIR consolidando TODAS as Observations em um único ObservationSummary.
    /// Isso resolve o problema de perda de dados (93-95%) ao processar apenas a primeira observation.
    /// </summary>
    private ObservationSummary AnalyzeBundle(JsonElement bundleRoot, string? rawDataId, DateTime? receivedAt)
    {
        if (!bundleRoot.TryGetProperty("entry", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Bundle FHIR não contém array 'entry'");
        }

        var summary = new ObservationSummary
        {
            ObservationId = GetBundleId(bundleRoot),
            DataColeta = GetBundleTimestamp(bundleRoot) ?? receivedAt ?? DateTime.UtcNow
        };

        int observationCount = 0;
        DateTime? firstObservationDate = null;

        // Processa TODAS as Observations do Bundle
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("resource", out var resource))
                continue;

            if (!resource.TryGetProperty("resourceType", out var resourceType))
                continue;

            var resourceTypeStr = resourceType.GetString();

            // Processa Observations
            if (resourceTypeStr == "Observation")
            {
                observationCount++;
                
                // Extrai valores laboratoriais (acumula no summary)
                ExtractLabValues(resource, summary);
                
                // Captura a data da primeira observation se disponível
                if (!firstObservationDate.HasValue)
                {
                    firstObservationDate = GetEffectiveDateTime(resource);
                }
            }
            // Extrai município do Patient
            else if (resourceTypeStr == "Patient")
            {
                ExtractMunicipalityCodeFromPatient(resource, summary);
            }
        }

        if (observationCount == 0)
        {
            throw new ArgumentException("Bundle FHIR não contém nenhum recurso do tipo Observation");
        }

        // Usa data da primeira observation se disponível, senão usa timestamp do bundle
        if (firstObservationDate.HasValue)
        {
            summary.DataColeta = firstObservationDate.Value;
        }

        // Aplicar regras clínicas com TODOS os valores laboratoriais consolidados
        ApplyClinicalRules(summary);

        return summary;
    }

    /// <summary>
    /// Processa uma única Observation FHIR
    /// </summary>
    private ObservationSummary AnalyzeSingleObservation(JsonElement observationRoot, string? rawDataId, DateTime? receivedAt)
    {
        var summary = new ObservationSummary
        {
            ObservationId = GetObservationId(observationRoot),
            DataColeta = GetEffectiveDateTime(observationRoot) ?? receivedAt ?? DateTime.UtcNow
        };

        // Extrair valores laboratoriais
        ExtractLabValues(observationRoot, summary);
        
        // Aplicar regras clínicas
        ApplyClinicalRules(summary);
        
        // Tentar extrair município (limitado sem Bundle)
        ExtractMunicipalityCode(observationRoot, summary);

        return summary;
    }

    /// <summary>
    /// Obtém o ID do Bundle FHIR
    /// </summary>
    private string GetBundleId(JsonElement bundleRoot)
    {
        if (bundleRoot.TryGetProperty("id", out var idElement))
        {
            return $"Bundle/{idElement.GetString()}";
        }
        return $"Bundle/{Guid.NewGuid()}";
    }

    /// <summary>
    /// Obtém o timestamp do Bundle FHIR
    /// </summary>
    private DateTime? GetBundleTimestamp(JsonElement bundleRoot)
    {
        if (bundleRoot.TryGetProperty("timestamp", out var timestampElement))
        {
            if (DateTime.TryParse(timestampElement.GetString(), out var timestamp))
            {
                return timestamp.Kind == DateTimeKind.Utc 
                    ? timestamp 
                    : timestamp.ToUniversalTime();
            }
        }
        return null;
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
    /// Extrai valores laboratoriais de uma Observation FHIR.
    /// Processa tanto 'valueQuantity' (valor principal) quanto 'component' (valores múltiplos).
    /// </summary>
    private void ExtractLabValues(JsonElement fhirRoot, ObservationSummary summary)
    {
        // Primeiro, extrai valor principal da Observation (valueQuantity)
        var mainValue = ExtractMainValue(fhirRoot);
        var mainCode = ExtractMainCode(fhirRoot);
        
        if (mainValue.HasValue && !string.IsNullOrEmpty(mainCode))
        {
            MapLoincCodeToLabValue(mainCode, mainValue.Value, summary);
        }

        // Depois, processa componentes (se houver)
        if (fhirRoot.TryGetProperty("component", out var componentsElement) && 
            componentsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var component in componentsElement.EnumerateArray())
            {
                var code = ExtractComponentCode(component);
                var value = ExtractComponentValue(component);

                if (string.IsNullOrEmpty(code) || !value.HasValue)
                    continue;

                MapLoincCodeToLabValue(code, value.Value, summary);
            }
        }
    }

    /// <summary>
    /// Mapeia código LOINC para chave no LabValues do summary.
    /// Centraliza a lógica de mapeamento para evitar duplicação.
    /// </summary>
    private void MapLoincCodeToLabValue(string loincCode, decimal value, ObservationSummary summary)
    {
        switch (loincCode)
        {
            case LoincCodes.LEUCOCITOS:
                summary.LabValues["leucocitos"] = value;
                break;
            case LoincCodes.NEUTROFILOS:
            case LoincCodes.NEUTROFILOS_ALT:
                summary.LabValues["neutrofilos"] = value;
                break;
            case LoincCodes.BASTONETES:
            case LoincCodes.BASTONETES_ALT:
                summary.LabValues["bastonetes"] = value;
                break;
            case LoincCodes.BASTONETES_PCT:
                summary.LabValues["bastonetes_pct"] = value;
                break;
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
    /// Extrai o valor numérico de um componente FHIR e normaliza unidades.
    /// </summary>
    private decimal? ExtractComponentValue(JsonElement component)
    {
        if (component.TryGetProperty("valueQuantity", out var valueElement) &&
            valueElement.TryGetProperty("value", out var valueNumber) &&
            valueNumber.ValueKind == JsonValueKind.Number)
        {
            return NormalizeValueWithUnit(valueNumber.GetDecimal(), valueElement);
        }
        return null;
    }

    /// <summary>
    /// Normaliza valores laboratoriais baseado na unidade de medida.
    /// Converte todas as contagens celulares para células/μL (unidade padrão).
    /// </summary>
    private decimal NormalizeValueWithUnit(decimal value, JsonElement valueElement)
    {
        // Verifica a unidade para fazer conversão se necessário
        if (valueElement.TryGetProperty("code", out var unitCode))
        {
            var unit = unitCode.GetString();
            
            // Conversões de unidades para células/μL (padrão)
            switch (unit)
            {
                case "10*3/uL":
                case "x10*3/uL":
                case "10^3/uL":
                    // 10³/μL → células/μL: multiplica por 1000
                    return value * 1000m;
                    
                case "10*6/uL":
                case "x10*6/uL":
                case "10^6/uL":
                    // 10⁶/μL → células/μL: multiplica por 1.000.000
                    return value * 1000000m;
                    
                case "cells/uL":
                case "/uL":
                case "cells/μL":
                case "/μL":
                    // Já está em células/μL, sem conversão
                    return value;
                    
                case "%":
                    // Percentual - mantém como está (será tratado separadamente)
                    return value;
                    
                default:
                    // Unidade desconhecida - assume que já está normalizada
                    return value;
            }
        }
        
        return value;
    }

    /// <summary>
    /// Extrai o valor principal da observação FHIR com normalização de unidades
    /// </summary>
    private decimal? ExtractMainValue(JsonElement fhirRoot)
    {
        if (fhirRoot.TryGetProperty("valueQuantity", out var valueElement) &&
            valueElement.TryGetProperty("value", out var valueNumber) &&
            valueNumber.ValueKind == JsonValueKind.Number)
        {
            return NormalizeValueWithUnit(valueNumber.GetDecimal(), valueElement);
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
    /// Extrai o código do município IBGE do recurso Patient
    /// Mapeia cidade brasileira para código IBGE de 7 dígitos
    /// </summary>
    private void ExtractMunicipalityCodeFromPatient(JsonElement patientResource, ObservationSummary summary)
    {
        // Extrai endereço do paciente
        if (!patientResource.TryGetProperty("address", out var addresses) ||
            addresses.ValueKind != JsonValueKind.Array)
        {
            return;
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