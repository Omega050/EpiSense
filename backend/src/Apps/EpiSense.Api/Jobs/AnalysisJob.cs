using EpiSense.Analysis.Services;
using EpiSense.Analysis.Infrastructure;
using MongoDB.Bson;

namespace EpiSense.Api.Jobs;

/// <summary>
/// Job do Hangfire para processar análise assíncrona de dados FHIR
/// </summary>
public class AnalysisJob
{
    private readonly FhirAnalysisService _analysisService;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly ILogger<AnalysisJob> _logger;

    public AnalysisJob(
        FhirAnalysisService analysisService,
        IAnalysisRepository analysisRepository,
        ILogger<AnalysisJob> logger)
    {
        _analysisService = analysisService;
        _analysisRepository = analysisRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executa a análise de um recurso FHIR
    /// </summary>
    /// <param name="fhirJson">JSON do recurso FHIR</param>
    /// <param name="rawDataId">ID do dado bruto no MongoDB</param>
    /// <param name="receivedAt">Data de recebimento do dado</param>
    public async Task ProcessAnalysisAsync(string fhirJson, string rawDataId, DateTime receivedAt)
    {
        try
        {
            _logger.LogInformation("🔄 Iniciando análise assíncrona para rawDataId: {RawDataId}", rawDataId);

            // Garantir que receivedAt seja UTC
            var receivedAtUtc = receivedAt.Kind == DateTimeKind.Utc
                ? receivedAt
                : receivedAt.ToUniversalTime();

            // Executar análise
            var summary = _analysisService.AnalyzeObservation(
                fhirJson: fhirJson,
                rawDataId: rawDataId,
                receivedAt: receivedAtUtc
            );

            // Salvar resultado
            await _analysisRepository.SaveAsync(summary);

            _logger.LogInformation(
                "✅ Análise concluída com sucesso para rawDataId: {RawDataId} - {FlagCount} flags detectadas",
                rawDataId,
                summary.Flags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Erro ao processar análise para rawDataId: {RawDataId}",
                rawDataId);
            throw; // Hangfire irá fazer retry automaticamente
        }
    }
}
