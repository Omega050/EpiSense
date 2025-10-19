namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Flags clínicas para detecção de padrões epidemiológicos baseadas em evidências
/// </summary>
public static class ClinicalFlags
{
    // Flags individuais - Dengue
    public const string TROMBOCITOPENIA = "TROMBOCITOPENIA";           // < 100.000/mm³
    public const string LEUCOPENIA_INTENSA = "LEUCOPENIA_INTENSA";     // < 2.000/mm³
    public const string LEUCOPENIA_MODERADA = "LEUCOPENIA_MODERADA";   // < 4.000/mm³
    public const string HEMOCONCENTRACAO = "HEMOCONCENTRACAO";         // Aumento 20% hematócrito ou >40% (M), >45% (H)
    
    // Flags individuais - Anemia
    public const string HEMOGLOBINA_BAIXA = "HEMOGLOBINA_BAIXA";       // < 12 g/dL (M), < 13.6 g/dL (H)
    public const string MICROCITOSE = "MICROCITOSE";                   // VCM < 80 fL
    public const string ANISOCITOSE = "ANISOCITOSE";                   // RDW elevado
    
    // Flags compostas - Padrões clínicos
    public const string DENGUE = "DENGUE";                             // Trombocitopenia + Leucopenia + Hemoconcentração
    public const string ANEMIA = "ANEMIA";                             // Hemoglobina baixa + Microcitose + Anisocitose
    
    /// <summary>
    /// Todas as flags disponíveis
    /// </summary>
    public static readonly string[] AllFlags = {
        TROMBOCITOPENIA,
        LEUCOPENIA_INTENSA,
        LEUCOPENIA_MODERADA,
        HEMOCONCENTRACAO,
        HEMOGLOBINA_BAIXA,
        MICROCITOSE,
        ANISOCITOSE,
        DENGUE,
        ANEMIA
    };
}

/// <summary>
/// Códigos LOINC para identificação de componentes laboratoriais
/// Baseado em evidências científicas para detecção de dengue e anemia
/// </summary>
public static class LoincCodes
{
    // Parâmetros para Dengue
    public const string PLAQUETAS = "777-3";           // Platelets [#/volume] in Blood
    public const string LEUCOCITOS = "6690-2";         // Leukocytes [#/volume] in Blood
    public const string HEMATOCRITO = "4544-3";        // Hematocrit [Volume Fraction] of Blood
    
    // Parâmetros para Anemia
    public const string HEMOGLOBINA = "718-7";         // Hemoglobin [Mass/volume] in Blood
    public const string VCM = "787-2";                 // MCV [Entitic volume] by Automated count
    public const string VCM_ALT = "789-8";             // Erythrocyte mean corpuscular volume (código alternativo)
    public const string RDW = "788-0";                 // RDW [Entitic volume] by Automated count
    
    // Painel completo
    public const string HEMOGRAMA_COMPLETO = "58410-2"; // CBC panel - Blood by Automated count
}

/// <summary>
/// Limiares clínicos baseados em evidências científicas
/// </summary>
public static class ClinicalThresholds
{
    // Dengue - Plaquetas
    public const decimal DENGUE_PLAQUETAS_BAIXAS = 100000m;  // < 100.000/mm³
    
    // Dengue - Leucócitos  
    public const decimal DENGUE_LEUCOPENIA_INTENSA = 2000m;  // < 2.000/mm³
    public const decimal DENGUE_LEUCOPENIA_MODERADA = 4000m; // < 4.000/mm³
    
    // Dengue - Hematócrito
    public const decimal HEMATOCRITO_MULHER_ALTO = 40m;      // > 40% mulheres
    public const decimal HEMATOCRITO_HOMEM_ALTO = 45m;       // > 45% homens
    public const decimal HEMATOCRITO_AUMENTO_PERCENTUAL = 20m; // Aumento de 20% sobre basal
    
    // Anemia - Hemoglobina
    public const decimal ANEMIA_HEMOGLOBINA_MULHER = 12.0m;   // < 12 g/dL mulheres
    public const decimal ANEMIA_HEMOGLOBINA_HOMEM = 13.6m;    // < 13.6 g/dL homens
    
    // Anemia - VCM (Volume Corpuscular Médio)
    public const decimal ANEMIA_VCM_MICROCITOSE = 80m;        // < 80 fL
}