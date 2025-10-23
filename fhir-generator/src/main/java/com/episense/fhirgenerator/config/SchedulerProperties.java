package com.episense.fhirgenerator.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

@Data
@Configuration
@ConfigurationProperties(prefix = "scheduler")
public class SchedulerProperties {

    /**
     * Enable or disable the automatic hemograma generation scheduler.
     */
    private Boolean enabled = true;

    /**
     * Initial delay in milliseconds before the first scheduler execution.
     */
    private Long initialDelay = 60000L;

    /**
     * Minimum interval in minutes between scheduler executions.
     */
    private Integer minIntervalMinutes = 3;

    /**
     * Maximum interval in minutes between scheduler executions.
     */
    private Integer maxIntervalMinutes = 10;

    /**
     * Minimum number of hemogramas to generate in each batch.
     */
    private Integer minBatchSize = 20;

    /**
     * Maximum number of hemogramas to generate in each batch.
     */
    private Integer maxBatchSize = 100;

}
