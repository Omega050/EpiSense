package com.episense.fhirgenerator.scheduler;

import com.episense.fhirgenerator.config.AnomalyProperties;
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

/**
 * Scheduler for automatic FHIR hemograma generation with anomaly injection.
 * 
 * <p>Uses the smart batch generation strategy:</p>
 * <ul>
 *   <li>25% of cases are anomalies (configurable)</li>
 *   <li>70% of anomalies are concentrated in outbreak cities</li>
 *   <li>20% of batches trigger burst mode (3x volume, 80% anomaly rate)</li>
 *   <li>Bursts target D-2 for immediate Shewhart analysis</li>
 * </ul>
 */
@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnProperty(name = "scheduler.enabled", havingValue = "true", matchIfMissing = true)
public class HemogramaGeneratorScheduler {

    private final HemogramaService hemogramaService;
    private final ExternalApiService externalApiService;
    private final SchedulerProperties schedulerProperties;
    private final AnomalyProperties anomalyProperties;
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
            // Gerar quantidade aleatória de hemogramas
            int batchSize = schedulerProperties.getMinBatchSize() +
                    random.nextInt(schedulerProperties.getMaxBatchSize() - schedulerProperties.getMinBatchSize() + 1);

            log.info("===== Starting hemograma generation batch =====");
            log.info("Anomaly config: enabled={}, rate={}%, severe={}%, burst={}%", 
                    anomalyProperties.isEnabled(),
                    (int)(anomalyProperties.getPercentage() * 100),
                    (int)(anomalyProperties.getSevereRatio() * 100),
                    (int)(anomalyProperties.getBurstProbability() * 100));
            
            // Use smart batch generation (handles bursts and concentration automatically)
            List<Hemograma> hemogramas = hemogramaService.generateSmartBatch(batchSize);
            log.info("Generated and saved {} hemogramas to ScyllaDB", hemogramas.size());

            // Enviar para API externa
            externalApiService.sendHemogramas(hemogramas);
            log.info("Sent {} hemogramas to external API", hemogramas.size());

            // Calcular próximo intervalo aleatório
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
