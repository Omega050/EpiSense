package com.episense.fhirgenerator.service;

import ca.uhn.fhir.parser.IParser;
import com.episense.fhirgenerator.config.AnomalyProperties;
import com.episense.fhirgenerator.entity.Hemograma;
import com.episense.fhirgenerator.model.HemogramaData;
import com.episense.fhirgenerator.repository.HemogramaRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.hl7.fhir.r4.model.*;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.util.*;

/**
 * Service for generating FHIR Hemograma data with automatic anomaly injection.
 * 
 * <h3>Anomaly Generation Strategy:</h3>
 * <p>To ensure Shewhart detection fires, anomalies are:</p>
 * <ul>
 *   <li>Generated at configurable rate (default 25%)</li>
 *   <li>Concentrated in specific "outbreak cities" (e.g., Trindade|GO)</li>
 *   <li>Include burst periods with 80% anomaly rate and 3x volume</li>
 *   <li>Generate SIB_GRAVE (30%) and SIB_SUSPEITA (70%) patterns</li>
 * </ul>
 * 
 * <h3>Clinical Flag Triggers:</h3>
 * <ul>
 *   <li><b>SIB_SUSPEITA:</b> Leucócitos > 11,000 AND Neutrófilos > 7,500</li>
 *   <li><b>SIB_GRAVE:</b> Neutrófilos > 7,500 AND Bastonetes > 500</li>
 * </ul>
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class HemogramaService {

    private final HemogramaRepository hemogramaRepository;
    private final IParser jsonParser;
    private final AnomalyProperties anomalyProperties;
    
    private final Random random = new Random();

    public Hemograma generateAndSaveHemograma(String patientId) {
        return generateAndSaveHemograma(patientId, null, false, LocalDateTime.now());
    }

    public Hemograma generateAndSaveHemograma(String patientId, String city, boolean isSick, LocalDateTime date) {
        // log.info("Generating hemograma for patient: {} (Sick: {})", patientId, isSick);

        HemogramaData data = generateRandomHemogramaData(patientId, city, isSick, date);
        Bundle bundle = createHemogramaBundle(data);
        String fhirJson = jsonParser.encodeResourceToString(bundle);

        Hemograma hemograma = Hemograma.builder()
                .id(UUID.randomUUID())
                .patientId(data.getPatientId())
                .patientName(data.getPatientName())
                .city(data.getCity())
                .collectionDate(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant())
                .fhirBundleJson(fhirJson)
                .sentToApi(false)
                .createdAt(Instant.now())
                .redBloodCells(data.getRedBloodCells())
                .hemoglobin(data.getHemoglobin())
                .hematocrit(data.getHematocrit())
                .mcv(data.getMcv())
                .mch(data.getMch())
                .mchc(data.getMchc())
                .rdw(data.getRdw())
                .whiteBloodCells(data.getWhiteBloodCells())
                .neutrophils(data.getNeutrophils())
                .neutrophilsBandForm(data.getNeutrophilsBandForm())
                .lymphocytes(data.getLymphocytes())
                .monocytes(data.getMonocytes())
                .eosinophils(data.getEosinophils())
                .basophils(data.getBasophils())
                .platelets(data.getPlatelets())
                .mpv(data.getMpv())
                .build();

        return hemogramaRepository.save(hemograma);
    }

    public List<Hemograma> generateBatch(int count) {
        return generateBatch(count, "Sao Paulo|SP", 0.0); // Default to normal batch
    }

    public List<Hemograma> generateBatch(int count, String city, double anomalyRate) {
        return generateBatch(count, city, anomalyRate, LocalDateTime.now());
    }

    public List<Hemograma> generateBatch(int count, String city, double anomalyRate, LocalDateTime date) {
        log.info("Generating batch of {} hemogramas for {} (Anomaly Rate: {})", count, city, anomalyRate);

        return java.util.stream.IntStream.range(0, count)
                .mapToObj(_ -> {
                    boolean isSick = Math.random() < anomalyRate;
                    return generateAndSaveHemograma("PATIENT-" + UUID.randomUUID().toString().substring(0, 8), 
                            city, isSick, date);
                })
                .toList();
    }

    public void generateHistoricalData(String city, int days, int dailyCount) {
        generateHistoricalData(city, days, dailyCount, 0.01);
    }

    public void generateHistoricalData(String city, int days, int dailyCount, double anomalyRate) {
        log.info("Generating historical data for {} over {} days ({} per day) with anomaly rate {}", city, days, dailyCount, anomalyRate);
        
        LocalDateTime endDate = LocalDateTime.now().minusDays(1); // Until yesterday
        LocalDateTime startDate = endDate.minusDays(days);

        for (int i = 0; i < days; i++) {
            LocalDateTime currentDate = startDate.plusDays(i);
            // log.info("Generating data for date: {}", currentDate.toLocalDate());
            
            java.util.stream.IntStream.range(0, dailyCount).forEach(_ -> {
                // Use configurable anomaly rate
                boolean isSick = Math.random() < anomalyRate; 
                generateAndSaveHemograma("HIST-" + UUID.randomUUID().toString().substring(0, 8), 
                        city, isSick, currentDate);
            });
        }
        log.info("Historical data generation completed.");
    }

    public List<Hemograma> generateOutbreak(String city, int count, double anomalyRate) {
        return generateOutbreak(city, count, anomalyRate, LocalDateTime.now());
    }

    public List<Hemograma> generateOutbreak(String city, int count, double anomalyRate, LocalDateTime date) {
        log.info("Generating OUTBREAK batch for {} with {} items and {} anomaly rate at {}", city, count, anomalyRate, date);
        return generateBatch(count, city, anomalyRate, date);
    }

    public String generateDebugFhir() {
        HemogramaData data = generateRandomHemogramaData("DEBUG-PATIENT", "Sao Paulo|SP", true, LocalDateTime.now());
        Bundle bundle = createHemogramaBundle(data);
        return jsonParser.setPrettyPrint(true).encodeResourceToString(bundle);
    }

    public List<Hemograma> findNotSent() {
        return hemogramaRepository.findNotSent();
    }

    public void markAsSent(UUID id, int statusCode) {
        log.debug("Marking hemograma {} as sent with status code {}", id, statusCode);
        hemogramaRepository.updateSentStatus(id, true, Instant.now(), statusCode);
    }

    public List<Hemograma> findByPatientId(String patientId) {
        return hemogramaRepository.findByPatientId(patientId);
    }

    public long count() {
        return hemogramaRepository.count();
    }

    private Bundle createHemogramaBundle(HemogramaData data) {
        Bundle bundle = new Bundle();
        bundle.setId("bundle-" + data.getPatientId());
        bundle.setType(Bundle.BundleType.COLLECTION);
        bundle.setTimestamp(Date.from(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant()));

        String patientUuid = "urn:uuid:patient-" + data.getPatientId();
        String encounterUuid = "urn:uuid:encounter-" + data.getPatientId();
        String observationUuid = "urn:uuid:observation-" + data.getPatientId();

        // Patient Resource
        Patient patient = new Patient();
        patient.setId("patient-" + data.getPatientId());
        patient.addName()
                .setFamily(data.getPatientName())
                .setUse(org.hl7.fhir.r4.model.HumanName.NameUse.OFFICIAL);
        
        // Consolidated address with city, state and country
        if (data.getCity() != null || data.getState() != null) {
            Address address = patient.addAddress();
            address.setUse(Address.AddressUse.HOME);
            if (data.getCity() != null) {
                address.setCity(data.getCity());
            }
            if (data.getState() != null) {
                address.setState(data.getState());
            }
            address.setCountry("BRA");
        }
        bundle.addEntry()
                .setFullUrl(patientUuid)
                .setResource(patient);

        // Encounter Resource (clinical context)
        Encounter encounter = new Encounter();
        encounter.setId("encounter-" + data.getPatientId());
        encounter.setStatus(Encounter.EncounterStatus.FINISHED);
        encounter.getClass_()
                .setSystem("http://terminology.hl7.org/CodeSystem/v3-ActCode")
                .setCode("AMB")
                .setDisplay("ambulatory");
        encounter.getSubject().setReference(patientUuid);
        encounter.getPeriod()
                .setStart(Date.from(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant()))
                .setEnd(Date.from(data.getCollectionDate().plusMinutes(30).atZone(ZoneId.systemDefault()).toInstant()));
        bundle.addEntry()
                .setFullUrl(encounterUuid)
                .setResource(encounter);

        // CBC Panel Observation with components (for Shewhart detection)
        // This is the KEY change: single Observation with component[] for SIB detection
        Observation cbcPanel = createCbcPanelObservation(data, patientUuid, encounterUuid);
        bundle.addEntry()
                .setFullUrl(observationUuid)
                .setResource(cbcPanel);

        return bundle;
    }

    /**
     * Creates a CBC (Complete Blood Count) Panel Observation with components.
     * This structure is required for proper SIB detection (Leucocitose + Neutrofilia)
     * as the backend consolidates values from component[] array.
     */
    private Observation createCbcPanelObservation(HemogramaData data, String patientRef, String encounterRef) {
        Observation observation = new Observation();
        observation.setId("cbc-" + data.getPatientId());
        observation.setStatus(Observation.ObservationStatus.FINAL);

        // Category - Laboratory
        observation.addCategory()
                .addCoding()
                .setSystem("http://terminology.hl7.org/CodeSystem/observation-category")
                .setCode("laboratory")
                .setDisplay("Laboratory");

        // Code - CBC Panel (58410-2)
        observation.getCode()
                .addCoding()
                .setSystem("http://loinc.org")
                .setCode("58410-2")
                .setDisplay("Complete blood count (CBC) panel - Blood by Automated count");
        observation.getCode().setText("Complete Blood Count");

        // Subject and Encounter references
        observation.getSubject().setReference(patientRef);
        observation.getEncounter().setReference(encounterRef);

        // Effective DateTime
        observation.setEffective(new DateTimeType(
                Date.from(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant())));

        // === COMPONENTS - Critical for Shewhart SIB detection ===
        
        // Leukocytes (6690-2) - Required for Leucocitose detection
        if (data.getWhiteBloodCells() != null) {
            addObservationComponent(observation, "6690-2", "Leukocytes [#/volume] in Blood",
                    data.getWhiteBloodCells(), "cells/uL", 4000.0, 11000.0);
        }

        // Neutrophils (751-8) - Required for Neutrofilia detection
        if (data.getNeutrophils() != null) {
            addObservationComponent(observation, "751-8", "Neutrophils [#/volume] in Blood",
                    data.getNeutrophils(), "cells/uL", 2000.0, 7500.0);
        }

        // Band Forms/Stabs (764-1) - Required for Desvio à Esquerda detection
        if (data.getNeutrophilsBandForm() != null) {
            addObservationComponent(observation, "764-1", "Neutrophils.band form [#/volume] in Blood",
                    data.getNeutrophilsBandForm(), "cells/uL", 0.0, 500.0);
        }

        // Additional eritrograma components (for completeness)
        if (data.getRedBloodCells() != null) {
            addObservationComponent(observation, "789-8", "Erythrocytes [#/volume] in Blood",
                    data.getRedBloodCells() * 1000000, "cells/uL", 4500000.0, 5500000.0);
        }
        if (data.getHemoglobin() != null) {
            addObservationComponent(observation, "718-7", "Hemoglobin [Mass/volume] in Blood",
                    data.getHemoglobin(), "g/dL", 13.0, 17.0);
        }
        if (data.getHematocrit() != null) {
            addObservationComponent(observation, "4544-3", "Hematocrit [Volume Fraction] of Blood",
                    data.getHematocrit(), "%", 40.0, 50.0);
        }

        // Platelets
        if (data.getPlatelets() != null) {
            addObservationComponent(observation, "777-3", "Platelets [#/volume] in Blood",
                    data.getPlatelets() * 1000, "cells/uL", 150000.0, 400000.0);
        }

        return observation;
    }

    /**
     * Adds a component to an Observation with LOINC code, value, unit and reference range.
     */
    private void addObservationComponent(Observation observation, String loincCode, String display,
            Double value, String unit, Double refLow, Double refHigh) {
        Observation.ObservationComponentComponent component = observation.addComponent();
        
        // Code
        component.getCode()
                .addCoding()
                .setSystem("http://loinc.org")
                .setCode(loincCode)
                .setDisplay(display);

        // Value
        Quantity quantity = new Quantity();
        quantity.setValue(value);
        quantity.setUnit(unit);
        quantity.setSystem("http://unitsofmeasure.org");
        quantity.setCode(unit);
        component.setValue(quantity);

        // Reference Range
        if (refLow != null || refHigh != null) {
            Observation.ObservationReferenceRangeComponent refRange = component.addReferenceRange();
            if (refLow != null) {
                refRange.setLow(new Quantity().setValue(refLow).setUnit(unit));
            }
            if (refHigh != null) {
                refRange.setHigh(new Quantity().setValue(refHigh).setUnit(unit));
            }
        }
    }

    private HemogramaData generateRandomHemogramaData(String patientId, String cityInput, boolean isSick, LocalDateTime date) {
        String city = cityInput;
        String state = null;

        if (city == null) {
            // Randomly select from normal cities (anomalies go to outbreak cities)
            List<String> cities = anomalyProperties.getNormalCities();
            String selection = cities.get(random.nextInt(cities.size()));
            String[] parts = selection.split("\\|");
            city = parts[0];
            state = parts.length > 1 ? parts[1] : null;
        } else if (city.contains("|")) {
            String[] parts = city.split("\\|");
            city = parts[0];
            state = parts[1];
        }

        // Determine anomaly type
        AnomalyType anomalyType = isSick ? determineAnomalyType() : AnomalyType.NORMAL;
        
        return buildHemogramaData(patientId, city, state, date, anomalyType);
    }

    /**
     * Enum representing the type of anomaly to generate.
     */
    public enum AnomalyType {
        NORMAL,           // All values within normal range
        LEUCOCYTOSIS,     // Only WBC elevated (triggers LAB_LEUCOCITOSE)
        SIB_SUSPEITA,     // Leucocytosis + Neutrophilia (triggers SIB_SUSPEITA)
        SIB_GRAVE         // Neutrophilia + Left Shift (triggers SIB_GRAVE, weight 2x)
    }

    /**
     * Determines the type of anomaly based on configuration ratios.
     * Priority for severe cases to maximize Shewhart detection (weight 2x).
     */
    private AnomalyType determineAnomalyType() {
        double roll = random.nextDouble();
        
        // severeRatio% of anomalies are SIB_GRAVE (e.g., 30%)
        if (roll < anomalyProperties.getSevereRatio()) {
            return AnomalyType.SIB_GRAVE;
        }
        // Remaining are SIB_SUSPEITA (e.g., 70%)
        return AnomalyType.SIB_SUSPEITA;
    }

    /**
     * Builds HemogramaData with specific anomaly pattern.
     */
    private HemogramaData buildHemogramaData(String patientId, String city, String state, 
                                              LocalDateTime date, AnomalyType anomalyType) {
        HemogramaData.HemogramaDataBuilder builder = HemogramaData.builder()
                .patientId(patientId)
                .patientName("Patient " + patientId)
                .city(city)
                .state(state)
                .collectionDate(date)
                // Eritrograma - always normal
                .redBloodCells(randomInRange(4.5, 5.5))
                .hemoglobin(randomInRange(13.0, 17.0))
                .hematocrit(randomInRange(40.0, 50.0))
                .mcv(randomInRange(80.0, 100.0))
                .mch(randomInRange(27.0, 32.0))
                .mchc(randomInRange(32.0, 36.0))
                .rdw(randomInRange(11.5, 14.5))
                // Plaquetas - always normal
                .platelets(randomInRange(150.0, 400.0))
                .mpv(randomInRange(7.5, 11.5))
                // Other leucogram
                .lymphocytes(randomInRange(20.0, 45.0))
                .monocytes(randomInRange(2.0, 10.0))
                .eosinophils(randomInRange(1.0, 6.0))
                .basophils(randomInRange(0.0, 2.0));

        // Apply anomaly-specific values
        switch (anomalyType) {
            case SIB_GRAVE:
                // Neutrophilia (>7500) + Left Shift (>500) = SIB_GRAVE (weight 2x)
                builder.whiteBloodCells(randomInRange(anomalyProperties.getLeucocytosisRange()[0], 
                        anomalyProperties.getLeucocytosisRange()[1])) // Can have leucocytosis too
                       .neutrophils(randomInRange(anomalyProperties.getNeutrophiliaRange()[0], 
                        anomalyProperties.getNeutrophiliaRange()[1]))
                       .neutrophilsBandForm(randomInRange(anomalyProperties.getLeftShiftRange()[0], 
                        anomalyProperties.getLeftShiftRange()[1]));
                break;
                
            case SIB_SUSPEITA:
                // Leucocytosis (>11000) + Neutrophilia (>7500) = SIB_SUSPEITA
                builder.whiteBloodCells(randomInRange(anomalyProperties.getLeucocytosisRange()[0], 
                        anomalyProperties.getLeucocytosisRange()[1]))
                       .neutrophils(randomInRange(anomalyProperties.getNeutrophiliaRange()[0], 
                        anomalyProperties.getNeutrophiliaRange()[1]))
                       .neutrophilsBandForm(randomInRange(0.0, 
                        anomalyProperties.getLeftShiftThreshold() - 50)); // Below threshold
                break;
                
            case LEUCOCYTOSIS:
                // Only leucocytosis - less specific
                builder.whiteBloodCells(randomInRange(anomalyProperties.getLeucocytosisRange()[0], 
                        anomalyProperties.getLeucocytosisRange()[1]))
                       .neutrophils(randomInRange(1800.0, anomalyProperties.getNeutrophiliaThreshold() - 100))
                       .neutrophilsBandForm(randomInRange(0.0, anomalyProperties.getLeftShiftThreshold() - 50));
                break;
                
            case NORMAL:
            default:
                // All values within normal range
                builder.whiteBloodCells(randomInRange(4000.0, anomalyProperties.getLeucocytosisThreshold() - 500))
                       .neutrophils(randomInRange(1800.0, anomalyProperties.getNeutrophiliaThreshold() - 500))
                       .neutrophilsBandForm(randomInRange(0.0, anomalyProperties.getLeftShiftThreshold() - 50));
                break;
        }

        return builder.build();
    }

    /**
     * Generates a batch specifically for outbreak cities with high anomaly concentration.
     * Used during burst periods to create strong signals for Shewhart detection.
     */
    public List<Hemograma> generateOutbreakBatch(int count, LocalDateTime targetDate) {
        if (anomalyProperties.getOutbreakCities().isEmpty()) {
            log.warn("No outbreak cities configured. Using default.");
            return generateBatch(count, "Trindade|GO", anomalyProperties.getBurstAnomalyRate(), targetDate);
        }

        List<Hemograma> results = new ArrayList<>();
        
        // Distribute cases across outbreak cities
        int citiesCount = anomalyProperties.getOutbreakCities().size();
        int casesPerCity = count / citiesCount;
        int remainder = count % citiesCount;

        for (int i = 0; i < citiesCount; i++) {
            String city = anomalyProperties.getOutbreakCities().get(i);
            int cityCount = casesPerCity + (i < remainder ? 1 : 0);
            
            log.info("🔥 Generating {} outbreak cases for {} (anomaly rate: {}%)", 
                    cityCount, city, (int)(anomalyProperties.getBurstAnomalyRate() * 100));
            
            results.addAll(generateBatch(cityCount, city, anomalyProperties.getBurstAnomalyRate(), targetDate));
        }

        return results;
    }

    /**
     * Generates batch with automatic anomaly distribution based on configuration.
     * Normal cities get normal rate, outbreak cities get higher rate.
     */
    public List<Hemograma> generateSmartBatch(int count) {
        return generateSmartBatch(count, LocalDateTime.now());
    }

    public List<Hemograma> generateSmartBatch(int count, LocalDateTime date) {
        List<Hemograma> results = new ArrayList<>();
        
        // Determine if this is a burst period
        boolean isBurst = random.nextDouble() < anomalyProperties.getBurstProbability();
        
        if (isBurst && anomalyProperties.isEnabled()) {
            log.warn("⚡⚡⚡ BURST PERIOD TRIGGERED - Concentrated outbreak generation ⚡⚡⚡");
            
            // Calculate burst size
            int burstSize = (int) (count * anomalyProperties.getBurstSizeMultiplier());
            
            // Target D-2 for immediate Shewhart analysis
            LocalDateTime targetDate = LocalDateTime.now().minusDays(2);
            
            results.addAll(generateOutbreakBatch(burstSize, targetDate));
            
            log.info("Burst complete: {} cases generated for outbreak cities at {}", 
                    results.size(), targetDate.toLocalDate());
        } else {
            // Normal generation with configured anomaly rate
            results.addAll(generateDistributedBatch(count, date));
        }

        return results;
    }

    /**
     * Generates batch distributed across normal and outbreak cities.
     * Outbreak cities receive proportionally more anomalies.
     */
    private List<Hemograma> generateDistributedBatch(int count, LocalDateTime date) {
        List<Hemograma> results = new ArrayList<>();
        
        List<String> allCities = new ArrayList<>();
        allCities.addAll(anomalyProperties.getNormalCities());
        allCities.addAll(anomalyProperties.getOutbreakCities());
        
        double baseAnomalyRate = anomalyProperties.isEnabled() ? anomalyProperties.getPercentage() : 0.0;
        
        for (int i = 0; i < count; i++) {
            // Select city - bias towards outbreak cities for anomalous cases
            boolean isAnomaly = random.nextDouble() < baseAnomalyRate;
            String city;
            
            if (isAnomaly && !anomalyProperties.getOutbreakCities().isEmpty()) {
                // 70% chance to send anomaly to outbreak city (concentration)
                if (random.nextDouble() < 0.7) {
                    city = anomalyProperties.getOutbreakCities()
                            .get(random.nextInt(anomalyProperties.getOutbreakCities().size()));
                } else {
                    city = allCities.get(random.nextInt(allCities.size()));
                }
            } else {
                city = allCities.get(random.nextInt(allCities.size()));
            }
            
            results.add(generateAndSaveHemograma(
                    "PAT-" + UUID.randomUUID().toString().substring(0, 8),
                    city, isAnomaly, date));
        }

        // Log summary
        long anomalyCount = results.stream()
                .filter(h -> h.getWhiteBloodCells() > anomalyProperties.getLeucocytosisThreshold())
                .count();
        log.info("Generated {} hemogramas ({} anomalies, {}%)", 
                results.size(), anomalyCount, (anomalyCount * 100) / Math.max(1, results.size()));

        return results;
    }

    private double randomInRange(double min, double max) {
        return Math.round((min + random.nextDouble() * (max - min)) * 100.0) / 100.0;
    }

}
