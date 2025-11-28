namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Resultado da análise de Shewhart para um município/flag/data específicos.
/// </summary>
public class ShewhartResult
{
    public string MunicipioIbge { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
    public int ObservedValue { get; set; }
    public BaselineStatistics? Baseline { get; set; }
    public bool AnomalyDetected { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public AnomalySeverity Severity { get; set; }
    public bool InsufficientData { get; set; }
    public string Message { get; set; } = string.Empty;
}
