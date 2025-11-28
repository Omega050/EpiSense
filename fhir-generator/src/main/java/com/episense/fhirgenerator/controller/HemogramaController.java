package com.episense.fhirgenerator.controller;

import com.episense.fhirgenerator.entity.Hemograma;
import com.episense.fhirgenerator.service.HemogramaService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Slf4j
@RestController
@RequestMapping("/api/v1/hemograma")
@RequiredArgsConstructor
public class HemogramaController {
    
    private final HemogramaService hemogramaService;
    private final com.episense.fhirgenerator.service.ExternalApiService externalApiService;
    
    @GetMapping("/patient/{patientId}")
    public ResponseEntity<List<Hemograma>> getHemogramasByPatient(
            @PathVariable String patientId) {
        log.info("Retrieving hemogramas for patient: {}", patientId);
        
        try {
            List<Hemograma> hemogramas = hemogramaService.findByPatientId(patientId);
            return ResponseEntity.ok(hemogramas);
        } catch (Exception e) {
            log.error("Error retrieving hemogramas", e);
            return ResponseEntity.status(500).build();
        }
    }
    
    @GetMapping("/stats")
    public ResponseEntity<Map<String, Object>> getStats() {
        log.info("Retrieving statistics");
        
        try {
            long total = hemogramaService.count();
            List<Hemograma> notSent = hemogramaService.findNotSent();
            
            Map<String, Object> stats = new HashMap<>();
            stats.put("total", total);
            stats.put("sent", total - notSent.size());
            stats.put("pending", notSent.size());
            
            return ResponseEntity.ok(stats);
        } catch (Exception e) {
            log.error("Error retrieving stats", e);
            return ResponseEntity.status(500).build();
        }
    }
    
    @GetMapping("/health")
    public ResponseEntity<Map<String, String>> healthCheck() {
        Map<String, String> response = new HashMap<>();
        response.put("status", "UP");
        response.put("service", "FHIR Hemograma Generator");
        return ResponseEntity.ok(response);
    }

    @GetMapping("/debug/fhir")
    public ResponseEntity<String> getDebugFhir() {
        log.info("Generating debug FHIR bundle");
        try {
            String fhirJson = hemogramaService.generateDebugFhir();
            return ResponseEntity.ok()
                    .header("Content-Type", "application/fhir+json")
                    .body(fhirJson);
        } catch (Exception e) {
            log.error("Error generating debug FHIR", e);
            return ResponseEntity.status(500).body("Error generating debug FHIR: " + e.getMessage());
        }
    }

    @PostMapping("/anomaly-scenario")
    public ResponseEntity<String> triggerAnomalyScenario(
            @RequestParam(defaultValue = "Sao Paulo|SP") String city,
            @RequestParam(defaultValue = "90") int baselineDays,
            @RequestParam(defaultValue = "50") int baselineDailyCount,
            @RequestParam(defaultValue = "0.05") double baselineAnomalyRate,
            @RequestParam(defaultValue = "100") int outbreakDailyCount) {
        
        log.info("Triggering anomaly scenario for {}", city);
        
        try {
            // 1. Generate baseline (up to today)
            hemogramaService.generateHistoricalData(city, baselineDays, baselineDailyCount, baselineAnomalyRate);
            
            // 2. Generate outbreak for D-2 (Target date of Shewhart Job)
            // The job analyzes D-2 to ensure data consolidation. To test immediately, we must inject data in the past.
            LocalDateTime targetDate = LocalDateTime.now().minusDays(2);
            hemogramaService.generateOutbreak(city, outbreakDailyCount, 0.9, targetDate); 
            
            // 3. Force send data to API immediately
            log.info("Force sending generated data to external API...");
            List<Hemograma> pending = hemogramaService.findNotSent();
            externalApiService.sendHemogramas(pending);
            
            return ResponseEntity.ok("Anomaly scenario generated and sent successfully for " + city + " targeting date " + targetDate.toLocalDate());
        } catch (Exception e) {
            log.error("Error generating anomaly scenario", e);
            return ResponseEntity.status(500).body("Error: " + e.getMessage());
        }
    }
    
}
