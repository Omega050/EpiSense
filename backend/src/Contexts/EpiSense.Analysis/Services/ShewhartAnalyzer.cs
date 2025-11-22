using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
using EpiSense.Analysis.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EpiSense.Analysis.Services;

public class ShewhartAnalyzer
{
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<ShewhartAnalyzer> _logger;
    
    // Configurações padrão do algoritmo
    private const int _defaultBaselineDays = 60; // 2 meses de histórico recomendado
    private const double _controlLimitSigma = 3.0; // Regra dos 3 Sigma (99.7% confiança)
    private const int _minimumCasesForAnalysis = 90; // Mínimo de casos no período baseline

    public ShewhartAnalyzer(
        IAnalysisRepository repository,
        ILogger<ShewhartAnalyzer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Executa análise de Shewhart para um município e flag clínica específicos.
    /// </summary>
    /// <param name="municipioIbge">Código IBGE do município (7 dígitos)</param>
    /// <param name="flag">Flag clínica a analisar (ex: "SIB_SUSPEITA", "DENGUE")</param>
    /// <param name="targetDate">Data-alvo para análise (padrão: data atual)</param>
    /// <param name="baselineDays">Dias de histórico para calcular baseline (padrão: 60)</param>
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
        
        if (baselineDays < 30)
            throw new ArgumentException("Período de baseline deve ser >= 30 dias", nameof(baselineDays));

        // 1. Calcula período de baseline (exclui o dia-alvo)
        var baselineStart = targetDate.Value.AddDays(-baselineDays);
        var baselineEnd = targetDate.Value.AddDays(-1); // Dia anterior ao alvo

        // 2. Busca dados históricos do período de baseline
        var baselineData = await GetDailyCasesAsync(
            municipioIbge, 
            flag, 
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
                Flag = flag,
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

        // 5. Busca contagem de casos do dia-alvo
        var targetCases = await GetDailyCasesAsync(municipioIbge, flag, targetDate.Value, targetDate.Value);
        var observedValue = targetCases.FirstOrDefault()?.CaseCount ?? 0;

        // 6. Detecta anomalia
        var anomalyType = DetectAnomaly(observedValue, baseline);
        var anomalyDetected = anomalyType != AnomalyType.None;

        var result = new ShewhartResult
        {
            MunicipioIbge = municipioIbge,
            Flag = flag,
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
    /// Agrupa por data para obter série temporal de casos/dia.
    /// </summary>
    private async Task<List<DailyCaseCount>> GetDailyCasesAsync(
        string municipioIbge,
        string flag,
        DateTime startDate,
        DateTime endDate)
    {
        // Busca todas as observações do período
        var observations = await _repository.GetByMunicipioAsync(
            municipioIbge,
            startDate,
            endDate);

        // Filtra por flag e agrupa por data
        var dailyCounts = observations
            .Where(obs => obs.Flags.Contains(flag))
            .GroupBy(obs => obs.DataColeta.Date)
            .Select(g => new DailyCaseCount
            {
                Date = g.Key,
                CaseCount = g.Count()
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
}
