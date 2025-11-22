namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Tipo de anomalia detectada pelo Shewhart.
/// </summary>
public enum AnomalyType
{
    /// <summary>Nenhuma anomalia detectada</summary>
    None,
    
    /// <summary>Aumento abrupto (Xᵢ > UCL) - Possível surto</summary>
    AbruptIncrease,
    
    /// <summary>Diminuição abrupta (Xᵢ < LCL) - Possível sub-notificação</summary>
    AbruptDecrease
}
