package com.episense.fhirgenerator.scheduler;

import com.episense.fhirgenerator.config.SchedulerProperties;
import com.episense.fhirgenerator.entity.Hemograma;
import com.episense.fhirgenerator.service.ExternalApiService;
import com.episense.fhirgenerator.service.HemogramaService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Random;
import java.util.concurrent.TimeUnit;

@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnProperty(name = "scheduler.enabled", havingValue = "true", matchIfMissing = true)
public class HemogramaGeneratorScheduler {

    private final HemogramaService hemogramaService;
    private final ExternalApiService externalApiService;
    private final SchedulerProperties schedulerProperties;
    private final Random random = new Random();

    private long lastExecutionTime = 0;
    private long nextExecutionDelay = 0;

    @Scheduled(fixedDelayString = "${scheduler.initial-delay}", initialDelayString = "${scheduler.initial-delay}")
    public void generateAndSendHemogramas() {
        // Verificar se já passou tempo suficiente desde a última execução
        long currentTime = System.currentTimeMillis();
        if (lastExecutionTime > 0 && (currentTime - lastExecutionTime) < nextExecutionDelay) {
            return;
        }

        try {
            // Gerar quantidade aleatória de hemogramas (20-100)
            int batchSize = schedulerProperties.getMinBatchSize() +
                    random.nextInt(schedulerProperties.getMaxBatchSize() - schedulerProperties.getMinBatchSize() + 1);

            log.info("===== Starting hemograma generation batch =====");
            log.info("Batch size: {} hemogramas", batchSize);

            // Gerar e salvar no ScyllaDB
            List<Hemograma> hemogramas = hemogramaService.generateBatch(batchSize);
            log.info("Generated and saved {} hemogramas to ScyllaDB", hemogramas.size());

            // Enviar para API externa
            externalApiService.sendHemogramas(hemogramas);
            log.info("Sent {} hemogramas to external API", hemogramas.size());

            // Calcular próximo intervalo aleatório (3-10 minutos)
            int nextIntervalMinutes = schedulerProperties.getMinIntervalMinutes() +
                    random.nextInt(schedulerProperties.getMaxIntervalMinutes()
                            - schedulerProperties.getMinIntervalMinutes() + 1);
            nextExecutionDelay = TimeUnit.MINUTES.toMillis(nextIntervalMinutes);
            lastExecutionTime = currentTime;

            log.info("Next execution in {} minutes", nextIntervalMinutes);
            log.info("===== Batch completed successfully =====");

        } catch (Exception e) {
            log.error("Error in hemograma generation/sending process", e);
            // Em caso de erro, tentar novamente em 1 minuto
            nextExecutionDelay = TimeUnit.MINUTES.toMillis(1);
        }
    }

    @Scheduled(fixedDelay = 5, timeUnit = TimeUnit.MINUTES)
    public void retryFailedHemogramas() {
        try {
            List<Hemograma> notSent = hemogramaService.findNotSent();
            if (!notSent.isEmpty()) {
                log.info("Retrying {} unsent hemogramas", notSent.size());
                externalApiService.sendHemogramas(notSent);
            }
        } catch (Exception e) {
            log.error("Error retrying failed hemogramas", e);
        }
    }

}
