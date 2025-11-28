package com.episense.fhirgenerator.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

import java.util.List;

/**
 * Configuration properties for automatic anomaly generation.
 * 
 * <p>These settings control how anomalies are generated to trigger
 * the Shewhart outbreak detection algorithm in the backend.</p>
 * 
 * <h3>Strategy for Shewhart Detection:</h3>
 * <ul>
 *   <li>Anomalies are concentrated in specific "outbreak cities" to create localized spikes</li>
 *   <li>Temporal bursts create sudden increases that exceed UCL (Upper Control Limit)</li>
 *   <li>SIB_GRAVE cases are weighted 2x in the backend aggregation</li>
 * </ul>
 */
@Data
@Configuration
@ConfigurationProperties(prefix = "episense.anomaly")
public class AnomalyProperties {

    /**
     * Enable or disable automatic anomaly generation.
     */
    private boolean enabled = true;

    /**
     * Percentage of cases that will be anomalous (0.0 to 1.0).
     * Default: 0.25 (25%) - sufficient to trigger Shewhart with concentrated outbreaks.
     */
    private double percentage = 0.25;

    /**
     * Ratio of severe anomalies (SIB_GRAVE) vs moderate (SIB_SUSPEITA).
     * SIB_GRAVE = Neutrofilia + Desvio à Esquerda (weight 2x in backend).
     * Default: 0.30 (30% of anomalies will be SIB_GRAVE).
     */
    private double severeRatio = 0.30;

    /**
     * Cities designated for outbreak concentration (pipe-separated city|state).
     * Concentrating anomalies in fewer cities increases the chance of Shewhart detection
     * by creating localized spikes above the Upper Control Limit (UCL = μ + 3σ).
     */
    private List<String> outbreakCities = List.of("Trindade|GO", "Goiania|GO");

    /**
     * Cities for normal (non-outbreak) data generation.
     */
    private List<String> normalCities = List.of(
            "Sao Paulo|SP",
            "Rio de Janeiro|RJ",
            "Belo Horizonte|MG",
            "Curitiba|PR",
            "Porto Alegre|RS",
            "Salvador|BA",
            "Brasilia|DF"
    );

    /**
     * Probability that a batch will be a concentrated outbreak burst (0.0 to 1.0).
     * When triggered, the batch will send extra cases to outbreak cities with high anomaly rate.
     * Default: 0.20 (20% of batches are bursts) - ensures periodic spikes for Shewhart.
     */
    private double burstProbability = 0.20;

    /**
     * Anomaly rate during burst periods (0.0 to 1.0).
     * Default: 0.80 (80%) - creates strong signal above baseline noise.
     */
    private double burstAnomalyRate = 0.80;

    /**
     * Multiplier for batch size during burst periods.
     * Default: 3.0 (3x normal batch size) - ensures case count exceeds UCL.
     */
    private double burstSizeMultiplier = 3.0;

    // === Clinical Thresholds (matching backend ClinicalFlags.ClinicalThresholds) ===
    
    /**
     * Threshold for Leucocytosis detection (WBC > 11,000 cells/μL).
     */
    private double leucocytosisThreshold = 11000.0;

    /**
     * Threshold for Neutrophilia detection (Neutrophils > 7,500 cells/μL).
     */
    private double neutrophiliaThreshold = 7500.0;

    /**
     * Threshold for Left Shift detection (Band forms > 500 cells/μL).
     */
    private double leftShiftThreshold = 500.0;

    // === Value Ranges for Anomaly Generation ===
    
    /**
     * Range for leucocyte values in anomalous cases [min, max].
     * Must exceed leucocytosisThreshold (11,000) to trigger LAB_LEUCOCITOSE.
     */
    private double[] leucocytosisRange = {12000.0, 30000.0};

    /**
     * Range for neutrophil values in anomalous cases [min, max].
     * Must exceed neutrophiliaThreshold (7,500) to trigger LAB_NEUTROFILIA.
     */
    private double[] neutrophiliaRange = {8000.0, 25000.0};

    /**
     * Range for band form values in severe anomalies [min, max].
     * Must exceed leftShiftThreshold (500) to trigger LAB_DESVIO_ESQUERDA.
     */
    private double[] leftShiftRange = {600.0, 2000.0};

    // === Historical Data Generation (for baseline) ===
    
    /**
     * Number of days of historical data to generate on startup.
     * Should be > 60 days to satisfy Shewhart baseline requirements.
     */
    private int historicalDays = 90;

    /**
     * Daily case count for historical data generation.
     */
    private int historicalDailyCount = 50;

    /**
     * Anomaly rate for historical/baseline data (should be low).
     * Lower rate creates stable baseline, making outbreaks more detectable.
     */
    private double historicalAnomalyRate = 0.05;
}
