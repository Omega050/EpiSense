package com.episense.fhirgenerator.service;

import com.episense.fhirgenerator.config.ExternalApiProperties;
import com.episense.fhirgenerator.entity.Hemograma;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.*;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import java.util.List;

@Slf4j
@Service
@RequiredArgsConstructor
public class ExternalApiService {

    private final RestTemplate restTemplate;
    private final HemogramaService hemogramaService;
    private final ExternalApiProperties externalApiProperties;

    public void sendHemogramas(List<Hemograma> hemogramas) {
        log.info("Sending {} hemogramas to external API", hemogramas.size());

        for (Hemograma hemograma : hemogramas) {
            try {
                sendSingleHemograma(hemograma);
            } catch (Exception e) {
                log.error("Error sending hemograma {}: {}", hemograma.getId(), e.getMessage());
            }
        }
    }

    private void sendSingleHemograma(Hemograma hemograma) {
        try {
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.APPLICATION_JSON);

            HttpEntity<String> request = new HttpEntity<>(hemograma.getFhirBundleJson(), headers);

            ResponseEntity<String> response = restTemplate.exchange(
                    externalApiProperties.getUrl(),
                    HttpMethod.POST,
                    request,
                    String.class);

            if (response.getStatusCode().is2xxSuccessful()) {
                log.info("Successfully sent hemograma {} - Status: {}",
                        hemograma.getId(), response.getStatusCode());
                hemogramaService.markAsSent(hemograma.getId(), response.getStatusCode().value());
            } else {
                log.warn("Failed to send hemograma {} - Status: {}",
                        hemograma.getId(), response.getStatusCode());
            }

        } catch (Exception e) {
            log.error("Exception sending hemograma {}: {}", hemograma.getId(), e.getMessage());
            throw e;
        }
    }

}
