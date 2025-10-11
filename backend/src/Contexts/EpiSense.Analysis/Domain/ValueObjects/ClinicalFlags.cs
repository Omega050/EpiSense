namespace EpiSense.Analysis.Domain.ValueObjects;

/// <summary>
/// Flags clínicas para detecção de padrões epidemiológicos
/// </summary>
public static class ClinicalFlags
{
    // Flags para contagem de leucócitos
    public const string LEUCOPENIA = "LEUCOPENIA";           // < 4000 células/μL
    public const string LEUCOCITOSE = "LEUCOCITOSE";         // > 11000 células/μL
    
    // Flags para contagem de plaquetas
    public const string TROMBOCITOPENIA = "TROMBOCITOPENIA"; // < 150000 plaquetas/μL
    public const string TROMBOCITOSE = "TROMBOCITOSE";       // > 450000 plaquetas/μL
    
    // Flags para hemoglobina
    public const string ANEMIA = "ANEMIA";                   // < 12 g/dL (mulheres), < 13 g/dL (homens)
    public const string POLICITEMIA = "POLICITEMIA";         // > 16 g/dL (mulheres), > 17 g/dL (homens)
    
    // Flags combinadas para padrões específicos
    public const string PADRAO_VIRAL = "PADRAO_VIRAL";       // Leucopenia + Trombocitopenia
    public const string PADRAO_BACTERIANO = "PADRAO_BACTERIANO"; // Leucocitose
    public const string PADRAO_DENGUE = "PADRAO_DENGUE";     // Trombocitopenia severa
    
    /// <summary>
    /// Todas as flags disponíveis
    /// </summary>
    public static readonly string[] AllFlags = {
        LEUCOPENIA, LEUCOCITOSE,
        TROMBOCITOPENIA, TROMBOCITOSE,
        ANEMIA, POLICITEMIA,
        PADRAO_VIRAL, PADRAO_BACTERIANO, PADRAO_DENGUE
    };
}

/// <summary>
/// Códigos LOINC para identificação de componentes laboratoriais
/// </summary>
public static class LoincCodes
{
    public const string LEUCOCITOS = "6690-2";    // Leukocytes [#/volume] in Blood
    public const string PLAQUETAS = "777-3";      // Platelets [#/volume] in Blood  
    public const string HEMOGLOBINA = "718-7";    // Hemoglobin [Mass/volume] in Blood
    public const string HEMATOCRITO = "4544-3";   // Hematocrit [Volume Fraction] of Blood
    public const string HEMOGRAMA_COMPLETO = "58410-2"; // CBC panel - Blood by Automated count
}