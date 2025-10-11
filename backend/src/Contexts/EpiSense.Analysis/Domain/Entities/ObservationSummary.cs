using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EpiSense.Analysis.Domain.Entities;

/// <summary>
/// Resumo de observação processada para detecção de surtos
/// </summary>
[Table("observation_summaries")]
public class ObservationSummary
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// ID da observação FHIR original
    /// </summary>
    [Column("observation_id")]
    [Required]
    [StringLength(255)]
    public string ObservationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Data de coleta da amostra
    /// </summary>
    [Column("data_coleta")]
    public DateTime DataColeta { get; set; }
    
    /// <summary>
    /// Código do município IBGE para geolocalização
    /// </summary>
    [Column("codigo_municipio_ibge")]
    [StringLength(7)]
    public string? CodigoMunicipioIBGE { get; set; }
    
    /// <summary>
    /// Flags clínicas detectadas (JSON array)
    /// </summary>
    [Column("flags", TypeName = "jsonb")]
    public List<string> Flags { get; set; } = new();
    
    /// <summary>
    /// Data de processamento da análise
    /// </summary>
    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID da observação bruta no MongoDB
    /// </summary>
    [Column("raw_data_id")]
    [StringLength(255)]
    public string? RawDataId { get; set; }
    
    /// <summary>
    /// Valores laboratoriais principais (JSON)
    /// </summary>
    [Column("lab_values", TypeName = "jsonb")]
    public Dictionary<string, decimal> LabValues { get; set; } = new();
}
