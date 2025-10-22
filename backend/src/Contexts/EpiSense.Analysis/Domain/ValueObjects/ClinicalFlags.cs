namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Define os tipos de flags clínicas do sistema.
/// Separação clara entre alterações laboratoriais diretas e diagnósticos clínicos compostos.
/// </summary>
public static class ClinicalFlags
{
    /// <summary>
    /// Flags laboratoriais - Alterações diretas em exames individuais.
    /// Usadas para rastreamento e correlação, mas NÃO agregadas no cache diário (DailyCaseAggregation).
    /// Foco: Detecção de Síndrome de Infecção Bacteriana (SIB).
    /// </summary>
    public static class Laboratory
    {
        public const string LEUCOCITOSE = "LAB_LEUCOCITOSE";                   // > 11.000/μL
        public const string NEUTROFILIA = "LAB_NEUTROFILIA";                   // > 7.500/μL (absoluto)
        public const string DESVIO_ESQUERDA = "LAB_DESVIO_ESQUERDA";          // Bastonetes > 500/μL (> 10%)
        
        /// <summary>
        /// Lista todas as flags laboratoriais
        /// </summary>
        public static readonly string[] All = new[]
        {
            LEUCOCITOSE,
            NEUTROFILIA,
            DESVIO_ESQUERDA
        };
        
        /// <summary>
        /// Verifica se uma flag é laboratorial
        /// </summary>
        public static bool IsLaboratoryFlag(string flag) => 
            flag.StartsWith("LAB_") || All.Contains(flag);
    }

    /// <summary>
    /// Flags clínicas compostas - Diagnósticos inferidos a partir de múltiplos critérios clínicos.
    /// Estas flags SÃO agregadas no cache diário (DailyCaseAggregation) para detecção de surtos pelo Shewhart.
    /// Foco: Detecção de Síndrome de Infecção Bacteriana (SIB).
    /// </summary>
    public static class Clinical
    {
        public const string SIB_SUSPEITA = "SIB_SUSPEITA";                     // Leucocitose + Neutrofilia
        public const string SIB_GRAVE = "SIB_GRAVE";                           // Neutrofilia + Desvio à Esquerda
        
        /// <summary>
        /// Lista todas as flags clínicas compostas
        /// </summary>
        public static readonly string[] All = new[]
        {
            SIB_SUSPEITA,
            SIB_GRAVE
        };
        
        /// <summary>
        /// Verifica se uma flag é clínica composta
        /// </summary>
        public static bool IsClinicalFlag(string flag) => All.Contains(flag);
    }
    
    /// <summary>
    /// Obtém todas as flags (laboratoriais + clínicas)
    /// </summary>
    public static string[] GetAllFlags() => 
        Laboratory.All.Concat(Clinical.All).ToArray();
    
    /// <summary>
    /// Determina o tipo de uma flag
    /// </summary>
    public static FlagType GetFlagType(string flag)
    {
        if (Laboratory.IsLaboratoryFlag(flag)) return FlagType.Laboratory;
        if (Clinical.IsClinicalFlag(flag)) return FlagType.Clinical;
        return FlagType.Unknown;
    }
}

/// <summary>
/// Tipo de flag clínica
/// </summary>
public enum FlagType
{
    /// <summary>
    /// Flag desconhecida
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Flag laboratorial (alteração direta em exame)
    /// </summary>
    Laboratory,
    
    /// <summary>
    /// Flag clínica composta (diagnóstico inferido)
    /// </summary>
    Clinical
}

/// <summary>
/// Códigos LOINC para identificação de componentes laboratoriais
/// Baseado em evidências científicas para detecção de Síndrome de Infecção Bacteriana (SIB)
/// </summary>
public static class LoincCodes
{
    // Parâmetros para SIB
    public const string LEUCOCITOS = "6690-2";         // Leukocytes [#/volume] in Blood
    public const string NEUTROFILOS = "751-8";         // Neutrophils [#/volume] in Blood by Automated count
    public const string NEUTROFILOS_ALT = "753-4";     // Neutrophils [#/volume] in Blood (código alternativo)
    public const string BASTONETES = "764-1";          // Neutrophils.band form [#/volume] in Blood
    public const string BASTONETES_ALT = "711-2";      // Neutrophils.band form [#/volume] in Blood (código alternativo)
    public const string BASTONETES_PCT = "35332-6";    // Neutrophils.band form/100 leukocytes in Blood
    
    // Painel completo
    public const string HEMOGRAMA_COMPLETO = "58410-2"; // CBC panel - Blood by Automated count
    public const string DIFERENCIAL_LEUCOCITOS = "24318-8"; // White blood cell differential panel - Blood
}

/// <summary>
/// Limiares clínicos baseados em evidências científicas
/// Foco: Detecção de Síndrome de Infecção Bacteriana (SIB) em adultos
/// </summary>
public static class ClinicalThresholds
{
    // Leucócitos Totais
    public const decimal LEUCOCITOS_NORMAL_MIN = 4000m;          // 4.000 células/μL
    public const decimal LEUCOCITOS_NORMAL_MAX = 11000m;         // 11.000 células/μL
    public const decimal LEUCOCITOSE = 11000m;                   // > 11.000/μL (suspeita bacteriana)
    
    // Neutrófilos (Absoluto)
    public const decimal NEUTROFILOS_NORMAL_MIN = 2000m;         // 2.000 células/μL
    public const decimal NEUTROFILOS_NORMAL_MAX = 7500m;         // 7.500 células/μL
    public const decimal NEUTROFILIA = 7500m;                    // > 7.500/μL (suspeita bacteriana)
    public const decimal NEUTROFILIA_ALT = 8000m;                // > 8.000/μL (limiar alternativo)
    
    // Bastonetes (Bands) - Desvio à Esquerda
    public const decimal BASTONETES_NORMAL_MAX = 500m;           // 0 a 500 células/μL
    public const decimal BASTONETES_NORMAL_MAX_PCT = 5m;         // 0-5%
    public const decimal DESVIO_ESQUERDA = 500m;                 // > 500/μL (gravidade)
    public const decimal DESVIO_ESQUERDA_PCT = 10m;              // > 10% (gravidade)
}