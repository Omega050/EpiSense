namespace EpiSense.Analysis.Domain.Entities;

/// <summary>
/// Representa um resultado de análise epidemiológica
/// </summary>
public class AnalysisResult
{
    public Guid Id { get; set; }
    
    public string AnalysisType { get; set; } = string.Empty;
    
    public DateTime AnalyzedAt { get; set; }
    
    public string Region { get; set; } = string.Empty;
    
    public int CasesCount { get; set; }
    
    public double RiskScore { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
