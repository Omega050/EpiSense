namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Severidade da anomalia (baseado no desvio em sigmas).
/// </summary>
public enum AnomalySeverity
{
    /// <summary>Nenhuma anomalia</summary>
    None,
    
    /// <summary>Baixa (3-4σ) - Raro, requer atenção</summary>
    Low,
    
    /// <summary>Média (4-5σ) - Muito raro, requer investigação</summary>
    Medium,
    
    /// <summary>Alta (5-6σ) - Extremamente raro, requer ação imediata</summary>
    High,
    
    /// <summary>Crítica (>6σ) - Evento excepcional, alerta máximo</summary>
    Critical
}
