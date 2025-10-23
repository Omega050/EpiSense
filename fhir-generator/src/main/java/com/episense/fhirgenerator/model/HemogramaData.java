package com.episense.fhirgenerator.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class HemogramaData {
    
    private String patientId;
    private String patientName;
    private LocalDateTime collectionDate;
    
    // Eritrograma
    private Double redBloodCells;        // Hemácias (milhões/mm³)
    private Double hemoglobin;           // Hemoglobina (g/dL)
    private Double hematocrit;           // Hematócrito (%)
    private Double mcv;                  // VCM - Volume Corpuscular Médio (fL)
    private Double mch;                  // HCM - Hemoglobina Corpuscular Média (pg)
    private Double mchc;                 // CHCM - Concentração de Hemoglobina Corpuscular Média (%)
    private Double rdw;                  // RDW - Red Cell Distribution Width (%)
    
    // Leucograma
    private Double whiteBloodCells;      // Leucócitos (mil/mm³)
    private Double neutrophils;          // Neutrófilos (%)
    private Double lymphocytes;          // Linfócitos (%)
    private Double monocytes;            // Monócitos (%)
    private Double eosinophils;          // Eosinófilos (%)
    private Double basophils;            // Basófilos (%)
    
    // Plaquetas
    private Double platelets;            // Plaquetas (mil/mm³)
    private Double mpv;                  // MPV - Mean Platelet Volume (fL)
    
}
