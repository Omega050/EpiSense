using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpiSense.Analysis.Domain.ValueObjects;

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
    /// Valores laboratoriais principais (JSON)
    /// </summary>
    [Column("lab_values", TypeName = "jsonb")]
    public Dictionary<string, decimal> LabValues { get; set; } = new();
    
    // ==================== PROPRIEDADES COMPUTADAS ====================
    
    /// <summary>
    /// Indica se há suspeita de Síndrome de Infecção Bacteriana (SIB)
    /// Computed: true se contém flag SIB_SUSPEITA
    /// </summary>
    [NotMapped]
    public bool HasSibSuspeita => Flags.Contains(ClinicalFlags.Clinical.SIB_SUSPEITA);
    
    /// <summary>
    /// Indica se há suspeita de SIB Grave
    /// Computed: true se contém flag SIB_GRAVE
    /// </summary>
    [NotMapped]
    public bool HasSibGrave => Flags.Contains(ClinicalFlags.Clinical.SIB_GRAVE);
    
    /// <summary>
    /// Indica se há qualquer flag clínica de SIB (suspeita ou grave)
    /// Computed: true se contém SIB_SUSPEITA OU SIB_GRAVE
    /// </summary>
    [NotMapped]
    public bool HasAnySib => HasSibSuspeita || HasSibGrave;
}
