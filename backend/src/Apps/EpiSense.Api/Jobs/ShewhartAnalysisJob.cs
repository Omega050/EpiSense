using EpiSense.Analysis.Services;
using EpiSense.Analysis.Infrastructure;
using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;

namespace EpiSense.Api.Jobs;

/// <summary>
/// Job recorrente do Hangfire para análise epidemiológica usando algoritmo Shewhart.
/// Executa a cada 2 horas para detectar anomalias em dados consolidados (D-2).
/// </summary>
public class ShewhartAnalysisJob
{
    private readonly ShewhartAnalyzer _shewhartAnalyzer;
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<ShewhartAnalysisJob> _logger;

    // Configurações de análise
    private readonly string[] _flagsToAnalyze = new[]
    {
        "SIB_SUSPEITA",
        "SIB_GRAVE",
        "LEUCOCITOSE",
        "NEUTROFILIA",
        "DESVIO_ESQUERDA"
    };

    public ShewhartAnalysisJob(
        ShewhartAnalyzer shewhartAnalyzer,
        IAnalysisRepository repository,
        ILogger<ShewhartAnalysisJob> logger)
    {
        _shewhartAnalyzer = shewhartAnalyzer;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Executa análise Shewhart para todos os municípios e flags configurados.
    /// 
    /// ESTRATÉGIA TEMPORAL:
    /// - Baseline: D-60 até D-3 (histórico estável, 57 dias)
    /// - Target: D-2 (último dia consolidado)
    /// - Janela excluída (D-1, D-0): Dados frescos ainda em processamento
    /// 
    /// MOTIVO DA JANELA D-2:
    /// 1. Dados de D-0 e D-1 podem estar incompletos (processamento em andamento)
    /// 2. D-2 garante que todos os exames do dia foram processados e agregados
    /// 3. Evita falsos positivos/negativos por dados parciais
    /// </summary>
    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("🔬 Iniciando job de análise Shewhart (análise epidemiológica)...");

            // Define data-alvo: D-2 (dois dias atrás, dados consolidados)
            var targetDate = DateTime.UtcNow.Date.AddDays(-2);
            
            _logger.LogInformation(
                "📅 Analisando data: {TargetDate:yyyy-MM-dd} (D-2 para garantir dados consolidados)",
                targetDate);

            // Busca todos os municípios com dados no período
            var startDate = targetDate.AddDays(-60); // 60 dias de baseline
            var endDate = targetDate;
            var allObservations = await _repository.GetByDateRangeAsync(startDate, endDate);

            // Extrai lista única de municípios
            var municipios = allObservations
                .Select(obs => obs.CodigoMunicipioIBGE)
                .Where(ibge => !string.IsNullOrWhiteSpace(ibge))
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "🏙️  Encontrados {MunicipioCount} municípios com dados no período",
                municipios.Count);

            var totalAnalyses = 0;
            var totalAnomalies = 0;

            // Para cada combinação município + flag, executa análise
            foreach (var municipio in municipios)
            {
                foreach (var flag in _flagsToAnalyze)
                {
                    try
                    {
                        var result = await _shewhartAnalyzer.AnalyzeAsync(
                            municipioIbge: municipio,
                            flag: flag,
                            targetDate: targetDate,
                            baselineDays: 60 // Baseline de 60 dias (D-62 até D-3)
                        );

                        totalAnalyses++;

                        // Se anomalia detectada, registra log detalhado
                        if (result.AnomalyDetected)
                        {
                            totalAnomalies++;
                            
                            _logger.LogWarning(
                                "🚨 ANOMALIA: Município={MunicipioIbge}, Flag={Flag}, " +
                                "Data={TargetDate:yyyy-MM-dd}, Observado={ObservedValue}, " +
                                "Baseline=[μ={Mean:F1}, σ={StdDev:F1}], " +
                                "Limites=[{LCL:F1}, {UCL:F1}], " +
                                "Tipo={AnomalyType}, Severidade={Severity}",
                                municipio, flag, targetDate,
                                result.ObservedValue,
                                result.Baseline.Mean, result.Baseline.StdDev,
                                result.Baseline.LCL, result.Baseline.UCL,
                                result.AnomalyType, result.Severity);

                            // TODO: Persistir resultado para dashboard/alertas
                            // await SaveAnomalyResultAsync(result);
                        }
                        else if (result.InsufficientData)
                        {
                            _logger.LogDebug(
                                "⚠️  Dados insuficientes: Município={MunicipioIbge}, Flag={Flag}",
                                municipio, flag);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "❌ Erro ao analisar: Município={MunicipioIbge}, Flag={Flag}",
                            municipio, flag);
                        // Continua processamento dos demais
                    }
                }
            }

            _logger.LogInformation(
                "✅ Job Shewhart concluído: {TotalAnalyses} análises realizadas, " +
                "{TotalAnomalies} anomalias detectadas",
                totalAnalyses, totalAnomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro crítico ao executar job de análise Shewhart");
            throw; // Hangfire marca como falha
        }
    }

    /// <summary>
    /// Executa análise para um município e flag específicos (endpoint manual).
    /// </summary>
    public async Task<ShewhartResult> ExecuteForMunicipioAsync(string municipioIbge, string flag)
    {
        _logger.LogInformation(
            "🔬 Executando análise Shewhart manual: Município={MunicipioIbge}, Flag={Flag}",
            municipioIbge, flag);

        var targetDate = DateTime.UtcNow.Date.AddDays(-2);
        
        var result = await _shewhartAnalyzer.AnalyzeAsync(
            municipioIbge: municipioIbge,
            flag: flag,
            targetDate: targetDate,
            baselineDays: 60
        );

        if (result.AnomalyDetected)
        {
            _logger.LogWarning(
                "🚨 Anomalia detectada na análise manual: {Message}",
                result.Message);
        }

        return result;
    }
}
