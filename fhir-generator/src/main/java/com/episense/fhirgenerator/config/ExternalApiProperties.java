package com.episense.fhirgenerator.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Data
@Configuration
@ConfigurationProperties(prefix = "external.api")
public class ExternalApiProperties {

    /**
     * URL endpoint of the external API where FHIR hemograma bundles will be sent.
     */
    private String url = "http://localhost:5000/api/ingestion/fhir";

    /**
     * Timeout in milliseconds for HTTP requests to the external API.
     */
    private Long timeout = 30000L;

}
