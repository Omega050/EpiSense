package com.episense.fhirgenerator.controller;

import com.episense.fhirgenerator.entity.Hemograma;
import com.episense.fhirgenerator.service.HemogramaService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Slf4j
@RestController
@RequestMapping("/api/v1/hemograma")
@RequiredArgsConstructor
public class HemogramaController {
    
    private final HemogramaService hemogramaService;
    
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
    
}
