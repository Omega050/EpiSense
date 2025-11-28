namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Contagem diária de casos (estrutura interna para agregação temporal).
/// </summary>
public class DailyCaseCount
{
    public DateTime Date { get; set; }
    public int CaseCount { get; set; }
}
