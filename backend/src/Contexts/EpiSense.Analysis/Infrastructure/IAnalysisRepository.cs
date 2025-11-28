using EpiSense.Analysis.Domain.Entities;

namespace EpiSense.Analysis.Infrastructure;

/// <summary>
/// Repositório para persistência de análises clínicas
/// </summary>
public interface IAnalysisRepository
{
    /// <summary>
    /// Salva um resumo de observação analisada
    /// </summary>
    Task<ObservationSummary> SaveAsync(ObservationSummary summary);
    
    /// <summary>
    /// Busca um resumo por ID
    /// </summary>
    Task<ObservationSummary?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Busca todos os resumos de observações analisadas
    /// </summary>
    Task<IEnumerable<ObservationSummary>> GetAllAsync();
    
    /// <summary>
    /// Busca resumos por código IBGE do município
    /// </summary>
    Task<IEnumerable<ObservationSummary>> GetByMunicipioAsync(string codigoIbge, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Busca resumos que contenham alguma das flags especificadas
    /// </summary>
    Task<IEnumerable<ObservationSummary>> GetByFlagsAsync(string[] flags, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Busca resumos por período
    /// </summary>
    Task<IEnumerable<ObservationSummary>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Conta casos com flags específicas em um município
    /// </summary>
    Task<int> CountByMunicipioAndFlagsAsync(string codigoIbge, string[] flags, DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Busca agregações diárias para análise Shewhart (dados já consolidados com peso correto)
    /// </summary>
    Task<IEnumerable<DailyCaseAggregation>> GetDailyAggregationsAsync(string municipioIbge, string flag, DateTime startDate, DateTime endDate);
}
