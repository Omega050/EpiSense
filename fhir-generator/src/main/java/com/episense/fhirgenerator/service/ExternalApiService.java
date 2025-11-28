package com.episense.fhirgenerator.service;

import com.episense.fhirgenerator.config.ExternalApiProperties;
import com.episense.fhirgenerator.entity.Hemograma;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.*;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

@Slf4j
@Service
@RequiredArgsConstructor
public class ExternalApiService {

    private static final int THREAD_POOL_SIZE = 10;
    private static final int BATCH_SIZE = 50;
    private static final int DELAY_BETWEEN_BATCHES_MS = 100;

    private final RestTemplate restTemplate;
    private final HemogramaService hemogramaService;
    private final ExternalApiProperties externalApiProperties;

    private ExecutorService executorService;
    private Semaphore semaphore;

    @PostConstruct
    public void init() {
        executorService = Executors.newFixedThreadPool(THREAD_POOL_SIZE);
        semaphore = new Semaphore(THREAD_POOL_SIZE);
        log.info("ExternalApiService initialized with thread pool size: {}", THREAD_POOL_SIZE);
    }

    @PreDestroy
    public void destroy() {
        if (executorService != null) {
            executorService.shutdown();
            try {
                if (!executorService.awaitTermination(30, TimeUnit.SECONDS)) {
                    executorService.shutdownNow();
                }
            } catch (InterruptedException e) {
                executorService.shutdownNow();
                Thread.currentThread().interrupt();
            }
        }
    }

    public void sendHemogramas(List<Hemograma> hemogramas) {
        log.info("Sending {} hemogramas to external API in batches of {} (pool size: {})", 
                hemogramas.size(), BATCH_SIZE, THREAD_POOL_SIZE);

        long startTime = System.currentTimeMillis();
        AtomicInteger successCount = new AtomicInteger(0);
        AtomicInteger errorCount = new AtomicInteger(0);

        // Process in batches to avoid overwhelming the server
        List<List<Hemograma>> batches = partition(hemogramas, BATCH_SIZE);
        
        for (int i = 0; i < batches.size(); i++) {
            List<Hemograma> batch = batches.get(i);
            log.info("Processing batch {}/{} ({} items)", i + 1, batches.size(), batch.size());
            
            List<CompletableFuture<Void>> futures = batch.stream()
                    .map(hemograma -> CompletableFuture.runAsync(() -> {
                        try {
                            semaphore.acquire();
                            try {
                                sendSingleHemograma(hemograma);
                                successCount.incrementAndGet();
                            } finally {
                                semaphore.release();
                            }
                        } catch (InterruptedException e) {
                            Thread.currentThread().interrupt();
                            errorCount.incrementAndGet();
                        } catch (Exception e) {
                            errorCount.incrementAndGet();
                            log.error("Error sending hemograma {}: {}", hemograma.getId(), e.getMessage());
                        }
                    }, executorService))
                    .toList();

            // Wait for batch to complete
            CompletableFuture.allOf(futures.toArray(new CompletableFuture[0])).join();
            
            // Small delay between batches to let the server breathe
            if (i < batches.size() - 1) {
                try {
                    Thread.sleep(DELAY_BETWEEN_BATCHES_MS);
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                }
            }
        }

        long duration = System.currentTimeMillis() - startTime;
        log.info("Finished sending {} hemogramas in {}ms (success: {}, errors: {})", 
                hemogramas.size(), duration, successCount.get(), errorCount.get());
    }
    
    private <T> List<List<T>> partition(List<T> list, int size) {
        List<List<T>> partitions = new ArrayList<>();
        for (int i = 0; i < list.size(); i += size) {
            partitions.add(list.subList(i, Math.min(i + size, list.size())));
        }
        return partitions;
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
