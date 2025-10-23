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
        log.info("Generating random hemograma for patient: {}", patientId);

        HemogramaData data = generateRandomHemogramaData(patientId);
        Bundle bundle = createHemogramaBundle(data);
        String fhirJson = jsonParser.encodeResourceToString(bundle);

        Hemograma hemograma = Hemograma.builder()
                .id(UUID.randomUUID())
                .patientId(data.getPatientId())
                .patientName(data.getPatientName())
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
        log.info("Generating batch of {} hemogramas", count);

        return java.util.stream.IntStream.range(0, count)
                .mapToObj(_ -> generateAndSaveHemograma("PATIENT-" + UUID.randomUUID().toString().substring(0, 8)))
                .toList();
    }

    public List<Hemograma> findNotSent() {
        return hemogramaRepository.findNotSent();
    }

    public void markAsSent(UUID id, int statusCode) {
        hemogramaRepository.findById(id).ifPresent(hemograma -> {
            hemograma.setSentToApi(true);
            hemograma.setSentAt(Instant.now());
            hemograma.setApiResponseStatus(statusCode);
            hemogramaRepository.save(hemograma);
        });
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
                    data.getWhiteBloodCells(), "10*3/uL", "Leukocytes"));
        }
        if (data.getNeutrophils() != null) {
            bundle.addEntry().setResource(createObservation(data, "770-8", "Neutrophils",
                    data.getNeutrophils(), "%", "Neutrophils"));
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

    private HemogramaData generateRandomHemogramaData(String patientId) {
        return HemogramaData.builder()
                .patientId(patientId)
                .patientName("Patient " + patientId)
                .collectionDate(LocalDateTime.now())
                // Eritrograma - valores normais
                .redBloodCells(randomInRange(4.5, 5.5))
                .hemoglobin(randomInRange(13.0, 17.0))
                .hematocrit(randomInRange(40.0, 50.0))
                .mcv(randomInRange(80.0, 100.0))
                .mch(randomInRange(27.0, 32.0))
                .mchc(randomInRange(32.0, 36.0))
                .rdw(randomInRange(11.5, 14.5))
                // Leucograma - valores normais
                .whiteBloodCells(randomInRange(4.0, 11.0))
                .neutrophils(randomInRange(40.0, 70.0))
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
