namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Estatísticas do baseline histórico (período de referência).
/// </summary>
public class BaselineStatistics
{
    /// <summary>Média histórica (μ)</summary>
    public double Mean { get; set; }
    
    /// <summary>Desvio padrão histórico (σ)</summary>
    public double StdDev { get; set; }
    
    /// <summary>Upper Control Limit (μ + 3σ)</summary>
    public double UCL { get; set; }
    
    /// <summary>Lower Control Limit (μ - 3σ, mínimo 0)</summary>
    public double LCL { get; set; }
    
    /// <summary>Número de amostras usadas no cálculo</summary>
    public int SampleSize { get; set; }
    
    /// <summary>Período do baseline em dias</summary>
    public int BaselinePeriodDays { get; set; }
}
