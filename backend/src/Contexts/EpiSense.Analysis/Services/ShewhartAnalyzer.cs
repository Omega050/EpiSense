using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
using EpiSense.Analysis.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EpiSense.Analysis.Services;

/// <summary>
/// Analisador de anomalias epidemiológicas usando algoritmo de Shewhart (Controle Estatístico de Processos).
/// 
/// DECISÃO ARQUITETURAL (ADR-011):
/// - Usa dados agregados (DailyCaseAggregation) que já aplicam peso 2 para SIB_GRAVE
/// - SIB_GRAVE conta como 2 casos, SIB_SUSPEITA como 1 caso
/// - Todas as flags são normalizadas para SIB_SUSPEITA na agregação
/// </summary>
public class ShewhartAnalyzer
{
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<ShewhartAnalyzer> _logger;
    
    // Configurações padrão do algoritmo
    private const int _defaultBaselineDays = 15; // 15 dias de histórico recomendado
    private const double _controlLimitSigma = 3.0; // Regra dos 3 Sigma (99.7% confiança)
    private const int _minimumCasesForAnalysis = 10; // Mínimo de casos no período baseline

    public ShewhartAnalyzer(
        IAnalysisRepository repository,
        ILogger<ShewhartAnalyzer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Executa análise de Shewhart para um município e flag clínica específicos.
    /// 
    /// IMPORTANTE (ADR-011): Todas as flags relacionadas a SIB são normalizadas para SIB_SUSPEITA
    /// nas agregações diárias. Isso inclui: SIB_SUSPEITA, SIB_GRAVE, DESVIO_ESQUERDA, etc.
    /// </summary>
    /// <param name="municipioIbge">Código IBGE do município (7 dígitos)</param>
    /// <param name="flag">Flag clínica a analisar (ex: "SIB_SUSPEITA", "DESVIO_ESQUERDA")</param>
    /// <param name="targetDate">Data-alvo para análise (padrão: data atual)</param>
    /// <param name="baselineDays">Dias de histórico para calcular baseline (padrão: 15)</param>
    /// <returns>Resultado da análise com detecção de anomalia</returns>
    public async Task<ShewhartResult> AnalyzeAsync(
        string municipioIbge,
        string flag,
        DateTime? targetDate = null,
        int baselineDays = _defaultBaselineDays)
    {
        targetDate ??= DateTime.UtcNow.Date;
        
        _logger.LogInformation(
            "Iniciando análise Shewhart: Município={MunicipioIbge}, Flag={Flag}, Data={TargetDate}, Baseline={BaselineDays}d",
            municipioIbge, flag, targetDate, baselineDays);

        // Valida parâmetros
        if (string.IsNullOrWhiteSpace(municipioIbge) || municipioIbge.Length != 7)
            throw new ArgumentException("Código IBGE deve ter 7 dígitos", nameof(municipioIbge));
        
        if (string.IsNullOrWhiteSpace(flag))
            throw new ArgumentException("Flag clínica é obrigatória", nameof(flag));
        
        // Normaliza flag para busca nas agregações (ADR-011)
        var normalizedFlag = NormalizeFlagForAggregation(flag);
        
        if (baselineDays < 7)
            throw new ArgumentException("Período de baseline deve ser >= 7 dias", nameof(baselineDays));

        // 1. Calcula período de baseline (exclui o dia-alvo)
        var baselineStart = targetDate.Value.AddDays(-baselineDays);
        var baselineEnd = targetDate.Value.AddDays(-1); // Dia anterior ao alvo

        // 2. Busca dados históricos do período de baseline (usando flag normalizada)
        var baselineData = await GetDailyCasesAsync(
            municipioIbge, 
            normalizedFlag, 
            baselineStart, 
            baselineEnd);

        // 3. Valida volume mínimo de dados
        var totalBaselineCases = baselineData.Sum(d => d.CaseCount);
        if (totalBaselineCases < _minimumCasesForAnalysis)
        {
            _logger.LogWarning(
                "Dados insuficientes para análise: Município={MunicipioIbge}, Flag={Flag}, Casos={TotalCases} (mínimo: {MinCases})",
                municipioIbge, flag, totalBaselineCases, _minimumCasesForAnalysis);
            
            return new ShewhartResult
            {
                MunicipioIbge = municipioIbge,
                Flag = flag,  // Retorna flag original para o usuário
                TargetDate = targetDate.Value,
                AnomalyDetected = false,
                InsufficientData = true,
                Message = $"Dados insuficientes para análise confiável. Casos no baseline: {totalBaselineCases} (mínimo: {_minimumCasesForAnalysis})"
            };
        }

        // 4. Calcula baseline estatístico (μ e σ)
        var baseline = CalculateBaseline(baselineData);
        
        _logger.LogDebug(
            "Baseline calculado: μ={Mean:F2}, σ={StdDev:F2}, UCL={UCL:F2}, LCL={LCL:F2}",
            baseline.Mean, baseline.StdDev, baseline.UCL, baseline.LCL);

        // 5. Busca contagem de casos do dia-alvo (usando flag normalizada)
        var targetCases = await GetDailyCasesAsync(municipioIbge, normalizedFlag, targetDate.Value, targetDate.Value);
        var observedValue = targetCases.FirstOrDefault()?.CaseCount ?? 0;

        // 6. Detecta anomalia
        var anomalyType = DetectAnomaly(observedValue, baseline);
        var anomalyDetected = anomalyType != AnomalyType.None;

        var result = new ShewhartResult
        {
            MunicipioIbge = municipioIbge,
            Flag = flag,  // Retorna flag original para o usuário
            TargetDate = targetDate.Value,
            ObservedValue = observedValue,
            Baseline = baseline,
            AnomalyDetected = anomalyDetected,
            AnomalyType = anomalyType,
            InsufficientData = false
        };

        if (anomalyDetected)
        {
            var severity = CalculateSeverity(observedValue, baseline, anomalyType);
            result.Severity = severity;
            result.Message = FormatAnomalyMessage(observedValue, baseline, anomalyType, severity);
            
            _logger.LogWarning(
                "ANOMALIA DETECTADA: {Message}",
                result.Message);
        }
        else
        {
            result.Message = $"Valor observado ({observedValue}) dentro dos limites de controle [{baseline.LCL:F1}, {baseline.UCL:F1}]";
            
            _logger.LogInformation(
                "Nenhuma anomalia detectada: ObservedValue={ObservedValue}, LCL={LCL:F2}, UCL={UCL:F2}",
                observedValue, baseline.LCL, baseline.UCL);
        }

        return result;
    }

    /// <summary>
    /// Busca contagem diária de casos para um município, flag e período específicos.
    /// Usa os dados agregados (DailyCaseAggregation) que já aplicam peso 2 para SIB_GRAVE.
    /// </summary>
    private async Task<List<DailyCaseCount>> GetDailyCasesAsync(
        string municipioIbge,
        string flag,
        DateTime startDate,
        DateTime endDate)
    {
        // Busca agregações diárias do período (dados já consolidados com peso correto)
        var aggregations = await _repository.GetDailyAggregationsAsync(
            municipioIbge,
            flag,
            startDate,
            endDate);

        // Converte agregações para contagem diária
        var dailyCounts = aggregations
            .Select(agg => new DailyCaseCount
            {
                Date = agg.Data,
                CaseCount = agg.TotalCasos
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Preenche dias sem casos (zero cases)
        var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var completeSeries = allDates
            .Select(date => dailyCounts.FirstOrDefault(dc => dc.Date == date) 
                ?? new DailyCaseCount { Date = date, CaseCount = 0 })
            .ToList();

        return completeSeries;
    }

    /// <summary>
    /// Calcula baseline estatístico (média, desvio padrão e limites de controle).
    /// </summary>
    private BaselineStatistics CalculateBaseline(List<DailyCaseCount> data)
    {
        var values = data.Select(d => (double)d.CaseCount).ToList();
        
        // Calcula média (μ)
        var mean = values.Average();
        
        // Calcula desvio padrão (σ)
        var variance = values.Sum(x => Math.Pow(x - mean, 2)) / values.Count;
        var stdDev = Math.Sqrt(variance);
        
        // Calcula limites de controle
        var ucl = mean + (_controlLimitSigma * stdDev);
        var lcl = Math.Max(0, mean - (_controlLimitSigma * stdDev)); // LCL não pode ser negativo

        return new BaselineStatistics
        {
            Mean = mean,
            StdDev = stdDev,
            UCL = ucl,
            LCL = lcl,
            SampleSize = values.Count,
            BaselinePeriodDays = values.Count
        };
    }

    /// <summary>
    /// Detecta tipo de anomalia com base nos limites de controle.
    /// </summary>
    private AnomalyType DetectAnomaly(int observedValue, BaselineStatistics baseline)
    {
        if (observedValue > baseline.UCL)
            return AnomalyType.AbruptIncrease; // Surto súbito
        
        if (observedValue < baseline.LCL)
            return AnomalyType.AbruptDecrease; // Sub-notificação
        
        return AnomalyType.None;
    }

    /// <summary>
    /// Calcula severidade da anomalia baseado no desvio em relação aos limites.
    /// </summary>
    private AnomalySeverity CalculateSeverity(
        int observedValue,
        BaselineStatistics baseline,
        AnomalyType anomalyType)
    {
        if (anomalyType == AnomalyType.None)
            return AnomalySeverity.None;

        double deviationInSigmas;
        
        if (anomalyType == AnomalyType.AbruptIncrease)
        {
            // Quantos sigmas acima da média?
            deviationInSigmas = (observedValue - baseline.Mean) / baseline.StdDev;
        }
        else // AbruptDecrease
        {
            // Quantos sigmas abaixo da média?
            deviationInSigmas = (baseline.Mean - observedValue) / baseline.StdDev;
        }

        // Classificação de severidade
        if (deviationInSigmas >= 5.0)
            return AnomalySeverity.Critical;  // > 5σ: extremamente raro
        if (deviationInSigmas >= 4.0)
            return AnomalySeverity.High;      // 4-5σ: muito raro
        if (deviationInSigmas >= 3.0)
            return AnomalySeverity.Medium;    // 3-4σ: raro (padrão Shewhart)
        
        return AnomalySeverity.Low;
    }

    /// <summary>
    /// Formata mensagem descritiva da anomalia detectada.
    /// </summary>
    private string FormatAnomalyMessage(
        int observedValue,
        BaselineStatistics baseline,
        AnomalyType anomalyType,
        AnomalySeverity severity)
    {
        var deviationPercent = ((observedValue - baseline.Mean) / baseline.Mean) * 100;
        var deviationInSigmas = Math.Abs((observedValue - baseline.Mean) / baseline.StdDev);

        var direction = anomalyType == AnomalyType.AbruptIncrease ? "acima" : "abaixo";
        var interpretation = anomalyType == AnomalyType.AbruptIncrease 
            ? "Possível surto epidemiológico" 
            : "Possível sub-notificação ou evento incomum";

        return $"ANOMALIA {severity.ToString().ToUpper()}: " +
               $"Valor observado ({observedValue}) está {Math.Abs(deviationPercent):F1}% {direction} da média histórica ({baseline.Mean:F1}). " +
               $"Desvio: {deviationInSigmas:F1}σ. " +
               $"Limites de controle: [{baseline.LCL:F1}, {baseline.UCL:F1}]. " +
               $"{interpretation}.";
    }

    /// <summary>
    /// Normaliza flags clínicas para busca nas agregações diárias.
    /// 
    /// Conforme ADR-011, todas as flags relacionadas a SIB são agregadas como SIB_SUSPEITA:
    /// - SIB_SUSPEITA → SIB_SUSPEITA
    /// - SIB_GRAVE → SIB_SUSPEITA (com peso 2)
    /// - DESVIO_ESQUERDA → SIB_SUSPEITA
    /// - LAB_* → SIB_SUSPEITA (se relacionado a SIB)
    /// </summary>
    private string NormalizeFlagForAggregation(string flag)
    {
        // Flags relacionadas a SIB são todas agregadas como SIB_SUSPEITA
        var sibRelatedFlags = new[]
        {
            "SIB_SUSPEITA",
            "SIB_GRAVE",
            "DESVIO_ESQUERDA",
            "LAB_LEUCOCITOSE",
            "LAB_NEUTROFILIA",
            "LAB_DESVIO_ESQUERDA"
        };

        if (sibRelatedFlags.Contains(flag, StringComparer.OrdinalIgnoreCase))
        {
            return ClinicalFlags.Clinical.SIB_SUSPEITA;
        }

        // Outras flags (DENGUE, COVID, etc.) mantêm o nome original
        return flag;
    }
}
