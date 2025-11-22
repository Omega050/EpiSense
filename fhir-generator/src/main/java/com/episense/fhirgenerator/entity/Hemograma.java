package com.episense.fhirgenerator.entity;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import org.springframework.data.cassandra.core.cql.PrimaryKeyType;
import org.springframework.data.cassandra.core.mapping.Column;
import org.springframework.data.cassandra.core.mapping.PrimaryKeyColumn;
import org.springframework.data.cassandra.core.mapping.Table;

import java.time.Instant;
import java.util.UUID;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table("hemogramas")
public class Hemograma {
    
    @PrimaryKeyColumn(name = "id", ordinal = 0, type = PrimaryKeyType.PARTITIONED)
    private UUID id;
    
    @Column("patient_id")
    private String patientId;
    
    @Column("patient_name")
    private String patientName;

    @Column("city")
    private String city;
    
    @Column("collection_date")
    private Instant collectionDate;
    
    @Column("fhir_bundle_json")
    private String fhirBundleJson;
    
    @Column("sent_to_api")
    private Boolean sentToApi;
    
    @Column("sent_at")
    private Instant sentAt;
    
    @Column("api_response_status")
    private Integer apiResponseStatus;
    
    @Column("created_at")
    private Instant createdAt;
    
    // Eritrograma
    @Column("red_blood_cells")
    private Double redBloodCells;
    
    @Column("hemoglobin")
    private Double hemoglobin;
    
    @Column("hematocrit")
    private Double hematocrit;
    
    @Column("mcv")
    private Double mcv;
    
    @Column("mch")
    private Double mch;
    
    @Column("mchc")
    private Double mchc;
    
    @Column("rdw")
    private Double rdw;
    
    // Leucograma
    @Column("white_blood_cells")
    private Double whiteBloodCells;
    
    @Column("neutrophils")
    private Double neutrophils;

    @Column("neutrophils_band_form")
    private Double neutrophilsBandForm;
    
    @Column("lymphocytes")
    private Double lymphocytes;
    
    @Column("monocytes")
    private Double monocytes;
    
    @Column("eosinophils")
    private Double eosinophils;
    
    @Column("basophils")
    private Double basophils;
    
    // Plaquetas
    @Column("platelets")
    private Double platelets;
    
    @Column("mpv")
    private Double mpv;
    
}
