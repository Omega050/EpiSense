using EpiSense.Analysis.Services;
using EpiSense.Analysis.Infrastructure;

namespace EpiSense.Api.Jobs;

public class AggregationJob
{
    private readonly AggregationService _aggregationService;
    private readonly ILogger<AggregationJob> _logger;

    // 1️⃣ CONSTRUTOR: Recebe dependências (injetadas pelo Hangfire)
    public AggregationJob(
        AggregationService aggregationService,
        ILogger<AggregationJob> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    // 2️⃣ MÉTODO PÚBLICO: Executado pelo Hangfire (sem parâmetros)
    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("📊 Iniciando agregação diária...");
            
            // 3️⃣ CHAMA O SERVICE: Delega a lógica
            // IMPORTANTE: Agrega D-2 (não D-1) para evitar conflito com janela Shewhart
            // - D-0, D-1: Dados frescos (análise individual em tempo real)
            // - D-2 em diante: Agregados para baseline estatístico Shewhart
            var targetDate = DateTime.UtcNow.Date.AddDays(-2);
            
            _logger.LogInformation(
                "📅 Agregando data: {TargetDate:yyyy-MM-dd} (D-2 para estabilidade de baseline)",
                targetDate);
            
            await _aggregationService.UpdateDailyAggregationsAsync(targetDate);
            
            _logger.LogInformation("✅ Agregação diária concluída com sucesso");
        }
        catch (Exception ex)
        {
            // 4️⃣ TRATAMENTO DE ERRO: Log e re-throw
            _logger.LogError(ex, "❌ Erro ao executar agregação diária");
            throw; // Hangfire marca como falha
        }
    }
}