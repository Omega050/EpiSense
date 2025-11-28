package com.episense.fhirgenerator.config;

import com.episense.fhirgenerator.service.HemogramaService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class AnomalyScenarioRunner implements ApplicationRunner {

    private final HemogramaService hemogramaService;
    private final com.episense.fhirgenerator.service.ExternalApiService externalApiService;

    @Override
    public void run(ApplicationArguments args) throws Exception {
        log.info("Checking if historical data generation is needed...");

        long count = hemogramaService.count();
        if (count < 1000) { // Arbitrary threshold to detect "empty" or "fresh" DB
            log.info("Insufficient data detected (count: {}). Starting historical data generation...", count);
            
            // Generate 90 days of history, ~50 exams per day
            // This ensures enough baseline for Shewhart analysis (requires > 30 days, > 90 cases)
            // Using 5% anomaly rate to guarantee > 90 cases (50 * 90 * 0.05 = 225 cases)
            hemogramaService.generateHistoricalData("Sao Paulo|SP", 90, 50, 0.05);
            
            log.info("Historical data generation finished. Sending to API...");
            java.util.List<com.episense.fhirgenerator.entity.Hemograma> pending = hemogramaService.findNotSent();
            externalApiService.sendHemogramas(pending);
            
            log.info("Initial data sent to API.");
        } else {
            log.info("Sufficient data detected (count: {}). Skipping historical generation.", count);
        }
    }
}
