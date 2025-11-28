package com.episense.fhirgenerator.config;

import com.episense.fhirgenerator.entity.Hemograma;
import com.episense.fhirgenerator.service.ExternalApiService;
import com.episense.fhirgenerator.service.HemogramaService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

/**
 * Application startup runner that generates initial historical data for Shewhart baseline.
 * 
 * <p>On startup, if the database is empty or has insufficient data:</p>
 * <ul>
 *   <li>Generates 90 days of historical data (configurable)</li>
 *   <li>Uses low anomaly rate (5%) for stable baseline</li>
 *   <li>Injects concentrated outbreaks in target cities for D-2 (Shewhart analysis target)</li>
 *   <li>Ensures enough cases for statistical significance (μ, σ calculation)</li>
 * </ul>
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AnomalyScenarioRunner implements ApplicationRunner {

    private final HemogramaService hemogramaService;
    private final ExternalApiService externalApiService;
    private final AnomalyProperties anomalyProperties;

    @Override
    public void run(ApplicationArguments args) throws Exception {
        log.info("=== EpiSense FHIR Generator Startup ===");
        log.info("Anomaly generation: enabled={}, rate={}%, severe={}%",
                anomalyProperties.isEnabled(),
                (int)(anomalyProperties.getPercentage() * 100),
                (int)(anomalyProperties.getSevereRatio() * 100));
        log.info("Outbreak cities: {}", anomalyProperties.getOutbreakCities());
        log.info("Burst probability: {}%, rate: {}%, multiplier: {}x",
                (int)(anomalyProperties.getBurstProbability() * 100),
                (int)(anomalyProperties.getBurstAnomalyRate() * 100),
                anomalyProperties.getBurstSizeMultiplier());

        long count = hemogramaService.count();
        int minDataThreshold = anomalyProperties.getHistoricalDays() * anomalyProperties.getHistoricalDailyCount() / 2;

        if (count < minDataThreshold) {
            log.info("Insufficient data detected (count: {}, threshold: {}). Starting historical data generation...", 
                    count, minDataThreshold);
            
            generateInitialData();
            
            log.info("Initial data generation complete. Sending to API...");
            List<Hemograma> pending = hemogramaService.findNotSent();
            externalApiService.sendHemogramas(pending);
            log.info("Initial data sent to API ({} hemogramas).", pending.size());
        } else {
            log.info("Sufficient data detected (count: {}). Skipping historical generation.", count);
        }
        
        log.info("=== Startup complete. Scheduler will handle ongoing generation. ===");
    }

    /**
     * Generates initial data with:
     * 1. Baseline data for all cities (low anomaly rate)
     * 2. Concentrated outbreak data for D-2 in outbreak cities (high anomaly rate)
     */
    private void generateInitialData() {
        List<String> allCities = new ArrayList<>();
        allCities.addAll(anomalyProperties.getNormalCities());
        allCities.addAll(anomalyProperties.getOutbreakCities());

        // 1. Generate baseline data (historical, low anomaly rate)
        log.info("Phase 1: Generating {} days of baseline data ({} cases/day, {}% anomaly rate)...",
                anomalyProperties.getHistoricalDays(),
                anomalyProperties.getHistoricalDailyCount(),
                (int)(anomalyProperties.getHistoricalAnomalyRate() * 100));

        int casesPerCityPerDay = anomalyProperties.getHistoricalDailyCount() / allCities.size();
        
        for (String city : allCities) {
            hemogramaService.generateHistoricalData(
                    city,
                    anomalyProperties.getHistoricalDays(),
                    Math.max(5, casesPerCityPerDay), // At least 5 cases per city per day
                    anomalyProperties.getHistoricalAnomalyRate()
            );
        }

        // 2. Generate concentrated outbreak for D-2 (Shewhart target date)
        if (anomalyProperties.isEnabled() && !anomalyProperties.getOutbreakCities().isEmpty()) {
            log.info("Phase 2: Generating concentrated outbreak for D-2 (Shewhart analysis target)...");
            
            LocalDateTime targetDate = LocalDateTime.now().minusDays(2);
            int outbreakCasesPerCity = 100; // Strong signal for detection
            
            for (String city : anomalyProperties.getOutbreakCities()) {
                log.warn("🔥 Injecting {} outbreak cases for {} at {} ({}% anomaly rate)",
                        outbreakCasesPerCity, city, targetDate.toLocalDate(),
                        (int)(anomalyProperties.getBurstAnomalyRate() * 100));
                
                hemogramaService.generateOutbreak(
                        city,
                        outbreakCasesPerCity,
                        anomalyProperties.getBurstAnomalyRate(),
                        targetDate
                );
            }
        }

        log.info("Historical data generation completed.");
    }
}
