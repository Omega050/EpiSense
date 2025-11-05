using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EpiSense.Analysis.Domain.Entities;

/// <summary>
/// Cache de agregações diárias para otimização da detecção de anomalias.
/// Armazena contagens pré-calculadas APENAS de flags clínicas compostas por (município, data, flag).
/// 
/// IMPORTANTE: Apenas flags do tipo Clinical (ClinicalFlags.Clinical.*) são agregadas aqui.
/// Flags laboratoriais (ClinicalFlags.Laboratory.*) são ignoradas na agregação.
/// 
/// DECISÃO ARQUITETURAL (ADR-010):
/// Casos SIB_GRAVE são agregados como SIB_SUSPEITA para simplificação epidemiológica.
/// Portanto, o campo 'flag' sempre conterá "SIB_SUSPEITA" (não haverá registros com "SIB_GRAVE").
/// </summary>
[Table("daily_case_aggregations")]
public class DailyCaseAggregation
{
    /// <summary>
    /// Identificador único da agregação
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Código do município IBGE (7 dígitos)
    /// </summary>
    [Column("municipio_ibge")]
    [Required]
    [StringLength(7)]
    public string MunicipioIBGE { get; set; } = string.Empty;
    
    /// <summary>
    /// Data da agregação (apenas data, sem hora)
    /// </summary>
    [Column("data")]
    public DateTime Data { get; set; }
    
    /// <summary>
    /// Flag clínica composta agregada (ex: SIB_SUSPEITA, SIB_GRAVE).
    /// Deve ser uma flag do tipo ClinicalFlags.Clinical.*.
    /// </summary>
    [Column("flag")]
    [Required]
    [StringLength(100)]
    public string Flag { get; set; } = string.Empty;
    
    /// <summary>
    /// Total de casos detectados para esta combinação
    /// </summary>
    [Column("total_casos")]
    public int TotalCasos { get; set; }
    
    /// <summary>
    /// Data da última atualização do registro.
    /// Atualizado quando hemogramas são adicionados retroativamente ou quando a agregação é recalculada.
    /// Não há CreatedAt pois registros podem ser atualizados com dados retroativos.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
