package com.episense.fhirgenerator.config;

import lombok.RequiredArgsConstructor;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestTemplate;
import org.springframework.http.client.SimpleClientHttpRequestFactory;

@Configuration
@RequiredArgsConstructor
public class RestTemplateConfig {

    private final ExternalApiProperties externalApiProperties;

    @Bean
    public RestTemplate restTemplate(RestTemplateBuilder builder) {
        return builder
                .requestFactory(() -> {
                    SimpleClientHttpRequestFactory factory = new SimpleClientHttpRequestFactory();
                    int timeoutMillis = externalApiProperties.getTimeout().intValue();
                    factory.setConnectTimeout(timeoutMillis);
                    factory.setReadTimeout(timeoutMillis);
                    return factory;
                })
                .build();
    }

}
