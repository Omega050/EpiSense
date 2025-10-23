public class AggregationService
{
    private readonly AnalysisDbContext _context;
    
    // Método 1: Agregação completa (primeira execução ou reprocessamento)
    public async Task RebuildAllAggregationsAsync()
    {
        // Query: agrupa ObservationSummary por município + data + flag
        // Apenas flags do tipo ClinicalFlags.Clinical.*
        // INSERT ON CONFLICT UPDATE (usando índice único)
    }
    
    // Método 2: Agregação incremental (dados do dia anterior)
    public async Task UpdateDailyAggregationsAsync(DateTime targetDate)
    {
        // Busca apenas ObservationSummary.ProcessedAt >= targetDate
        // Atualiza somente registros do dia específico
    }
    
    // Método 3: Agregação retroativa (dados tardios)
    public async Task UpdateAggregationsForDateRangeAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        // Para casos de reprocessamento ou dados atrasados
    }
}