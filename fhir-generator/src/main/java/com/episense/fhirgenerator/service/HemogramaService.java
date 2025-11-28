package com.episense.fhirgenerator.service;

import ca.uhn.fhir.parser.IParser;
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
import java.util.Date;
import java.util.List;
import java.util.UUID;

@Slf4j
@Service
@RequiredArgsConstructor
public class HemogramaService {

    private final HemogramaRepository hemogramaRepository;
    private final IParser jsonParser;

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
        bundle.setType(Bundle.BundleType.COLLECTION);
        bundle.setTimestamp(Date.from(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant()));

        // Patient Resource
        Patient patient = new Patient();
        patient.setId(data.getPatientId());
        patient.addName().setFamily(data.getPatientName());
        if (data.getCity() != null) {
            patient.addAddress().setCity(data.getCity());
        }
        if (data.getState() != null) {
            patient.addAddress().setState(data.getState());
        }
        bundle.addEntry().setResource(patient);

        // Eritrograma
        if (data.getRedBloodCells() != null) {
            bundle.addEntry().setResource(createObservation(data, "789-8", "Red blood cells",
                    data.getRedBloodCells(), "10*6/uL", "Erythrocytes"));
        }
        if (data.getHemoglobin() != null) {
            bundle.addEntry().setResource(createObservation(data, "718-7", "Hemoglobin",
                    data.getHemoglobin(), "g/dL", "Hemoglobin"));
        }
        if (data.getHematocrit() != null) {
            bundle.addEntry().setResource(createObservation(data, "4544-3", "Hematocrit",
                    data.getHematocrit(), "%", "Hematocrit"));
        }
        if (data.getMcv() != null) {
            bundle.addEntry().setResource(createObservation(data, "787-2", "MCV",
                    data.getMcv(), "fL", "Mean Corpuscular Volume"));
        }
        if (data.getMch() != null) {
            bundle.addEntry().setResource(createObservation(data, "785-6", "MCH",
                    data.getMch(), "pg", "Mean Corpuscular Hemoglobin"));
        }
        if (data.getMchc() != null) {
            bundle.addEntry().setResource(createObservation(data, "786-4", "MCHC",
                    data.getMchc(), "g/dL", "Mean Corpuscular Hemoglobin Concentration"));
        }
        if (data.getRdw() != null) {
            bundle.addEntry().setResource(createObservation(data, "788-0", "RDW",
                    data.getRdw(), "%", "Red Cell Distribution Width"));
        }

        // Leucograma
        if (data.getWhiteBloodCells() != null) {
            bundle.addEntry().setResource(createObservation(data, "6690-2", "White blood cells",
                    data.getWhiteBloodCells(), "cells/uL", "Leukocytes"));
        }
        if (data.getNeutrophils() != null) {
            bundle.addEntry().setResource(createObservation(data, "751-8", "Neutrophils",
                    data.getNeutrophils(), "cells/uL", "Neutrophils"));
        }
        if (data.getNeutrophilsBandForm() != null) {
            bundle.addEntry().setResource(createObservation(data, "764-1", "Neutrophils.band form",
                    data.getNeutrophilsBandForm(), "cells/uL", "Band Forms"));
        }
        if (data.getLymphocytes() != null) {
            bundle.addEntry().setResource(createObservation(data, "736-9", "Lymphocytes",
                    data.getLymphocytes(), "%", "Lymphocytes"));
        }
        if (data.getMonocytes() != null) {
            bundle.addEntry().setResource(createObservation(data, "5905-5", "Monocytes",
                    data.getMonocytes(), "%", "Monocytes"));
        }
        if (data.getEosinophils() != null) {
            bundle.addEntry().setResource(createObservation(data, "713-8", "Eosinophils",
                    data.getEosinophils(), "%", "Eosinophils"));
        }
        if (data.getBasophils() != null) {
            bundle.addEntry().setResource(createObservation(data, "706-2", "Basophils",
                    data.getBasophils(), "%", "Basophils"));
        }

        // Plaquetas
        if (data.getPlatelets() != null) {
            bundle.addEntry().setResource(createObservation(data, "777-3", "Platelets",
                    data.getPlatelets(), "10*3/uL", "Platelets"));
        }
        if (data.getMpv() != null) {
            bundle.addEntry().setResource(createObservation(data, "32623-1", "MPV",
                    data.getMpv(), "fL", "Mean Platelet Volume"));
        }

        return bundle;
    }

    private Observation createObservation(HemogramaData data, String loincCode, String display,
            Double value, String unit, String text) {
        Observation observation = new Observation();
        observation.setStatus(Observation.ObservationStatus.FINAL);

        // Category
        observation.addCategory()
                .addCoding()
                .setSystem("http://terminology.hl7.org/CodeSystem/observation-category")
                .setCode("laboratory")
                .setDisplay("Laboratory");

        // Code (LOINC)
        observation.getCode()
                .addCoding()
                .setSystem("http://loinc.org")
                .setCode(loincCode)
                .setDisplay(display);
        observation.getCode().setText(text);

        // Subject (Patient)
        observation.getSubject()
                .setReference("Patient/" + data.getPatientId())
                .setDisplay(data.getPatientName());

        // Effective DateTime
        observation.setEffective(new DateTimeType(
                Date.from(data.getCollectionDate().atZone(ZoneId.systemDefault()).toInstant())));

        // Value
        Quantity quantity = new Quantity();
        quantity.setValue(value);
        quantity.setUnit(unit);
        quantity.setSystem("http://unitsofmeasure.org");
        quantity.setCode(unit);
        observation.setValue(quantity);

        return observation;
    }

    private HemogramaData generateRandomHemogramaData(String patientId, String cityInput, boolean isSick, LocalDateTime date) {
        String city = cityInput;
        String state = null;

        if (city == null) {
            String[] cities = {"Sao Paulo|SP", "Rio de Janeiro|RJ", "Belo Horizonte|MG", "Curitiba|PR", "Porto Alegre|RS"};
            String selection = cities[(int) (Math.random() * cities.length)];
            String[] parts = selection.split("\\|");
            city = parts[0];
            state = parts[1];
        } else if (city.contains("|")) {
            String[] parts = city.split("\\|");
            city = parts[0];
            state = parts[1];
        }

        return HemogramaData.builder()
                .patientId(patientId)
                .patientName("Patient " + patientId)
                .city(city)
                .state(state)
                .collectionDate(date)
                // Eritrograma - valores normais
                .redBloodCells(randomInRange(4.5, 5.5))
                .hemoglobin(randomInRange(13.0, 17.0))
                .hematocrit(randomInRange(40.0, 50.0))
                .mcv(randomInRange(80.0, 100.0))
                .mch(randomInRange(27.0, 32.0))
                .mchc(randomInRange(32.0, 36.0))
                .rdw(randomInRange(11.5, 14.5))
                // Leucograma - valores normais (Absolute counts)
                // Leucograma
                .whiteBloodCells(isSick ? randomInRange(12000.0, 25000.0) : randomInRange(4000.0, 11000.0)) // Leucocitose if sick
                .neutrophils(isSick ? randomInRange(8000.0, 20000.0) : randomInRange(1800.0, 7700.0)) // Neutrofilia if sick
                .neutrophilsBandForm(isSick ? randomInRange(600.0, 1500.0) : randomInRange(0.0, 500.0)) // Desvio a esquerda if sick
                .lymphocytes(randomInRange(20.0, 45.0))
                .monocytes(randomInRange(2.0, 10.0))
                .eosinophils(randomInRange(1.0, 6.0))
                .basophils(randomInRange(0.0, 2.0))
                // Plaquetas - valores normais
                .platelets(randomInRange(150.0, 400.0))
                .mpv(randomInRange(7.5, 11.5))
                .build();
    }

    private double randomInRange(double min, double max) {
        return Math.round((min + Math.random() * (max - min)) * 100.0) / 100.0;
    }

}
