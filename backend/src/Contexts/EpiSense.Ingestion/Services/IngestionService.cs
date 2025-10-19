using EpiSense.Ingestion.Domain;
using EpiSense.Ingestion.Infrastructure;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace EpiSense.Ingestion.Services;

public class IngestionService
{
    private readonly IIngestionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<IngestionService> _logger;
    
    // Delegate para análise clínica (injeção de dependência fraca)
    private readonly Func<RawHealthData, Task>? _analysisCallback;

    public IngestionService(
        IIngestionRepository repository,
        IEventPublisher eventPublisher,
        ILogger<IngestionService> logger,
        Func<RawHealthData, Task>? analysisCallback = null)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _analysisCallback = analysisCallback;
    }

    /// <summary>
    /// Salva dados brutos e dispara análise clínica automaticamente
    /// Pipeline: Receive → MongoDB (BSON) → Analysis (callback) → PostgreSQL
    /// </summary>
    public async Task<RawHealthData> IngestAndAnalyzeAsync(string fhirJson, string sourceSystem, string? sourceUrl = null)
    {
        // 1. Parse JSON para BsonDocument (valida e permite consultas no MongoDB)
        BsonDocument fhirBson;
        try
        {
            fhirBson = BsonDocument.Parse(fhirJson);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"JSON FHIR inválido: {ex.Message}", nameof(fhirJson), ex);
        }

        // 2. Criar entidade de dados brutos
        var rawData = new RawHealthData
        {
            FhirData = fhirBson,
            Metadata = new IngestionMetadata
            {
                SourceSystem = sourceSystem,
                SourceUrl = sourceUrl,
                ReceivedAt = DateTime.UtcNow,
                Status = IngestionStatus.Received
            }
        };

        try
        {
            // 3. Salvar no MongoDB como documento BSON
            await _repository.SaveRawDataAsync(rawData);
            _logger.LogInformation($"✓ Raw data saved to MongoDB as BSON document: {rawData.Id}");

            // 4. Chamar análise clínica (se disponível)
            if (_analysisCallback != null)
            {
                _logger.LogInformation($"→ Triggering clinical analysis for {rawData.Id}...");
                await _analysisCallback(rawData);
                
                // 5. Atualizar status para Processed após análise bem-sucedida
                rawData.Metadata.Status = IngestionStatus.Processed;
                await _repository.UpdateRawDataAsync(rawData);
                
                _logger.LogInformation($"✓ Clinical analysis completed for {rawData.Id}");
            }
            else
            {
                _logger.LogWarning("⚠ Analysis callback not configured - data saved but not analyzed");
            }

            await _eventPublisher.PublishDataIngestedAsync(rawData.Id!);
            return rawData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"✗ Failed to ingest/analyze data: {ex.Message}");
            
            rawData.Metadata.Status = IngestionStatus.Failed;
            rawData.Metadata.ErrorMessage = ex.Message;
            await _repository.UpdateRawDataAsync(rawData);
            
            await _eventPublisher.PublishDataValidationFailedAsync(rawData.Id!, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Processa dados pendentes em lote (para reprocessamento)
    /// </summary>
    public async Task ProcessIncomingDataAsync()
    {
        _logger.LogInformation("Starting batch processing of pending data...");
        
        var pendingData = await _repository.GetDataByStatusAsync(IngestionStatus.Received);
        var dataList = pendingData.ToList();
        
        _logger.LogInformation($"Found {dataList.Count} pending observations");

        int successCount = 0;
        int failureCount = 0;

        foreach (var rawData in dataList)
        {
            try
            {
                if (_analysisCallback != null)
                {
                    await _analysisCallback(rawData);
                    rawData.Metadata.Status = IngestionStatus.Processed;
                    await _repository.UpdateRawDataAsync(rawData);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"✗ Failed to process {rawData.Id}");
                rawData.Metadata.Status = IngestionStatus.Failed;
                rawData.Metadata.ErrorMessage = ex.Message;
                await _repository.UpdateRawDataAsync(rawData);
                failureCount++;
            }
        }

        _logger.LogInformation($"Batch processing completed: {successCount} succeeded, {failureCount} failed");
    }

    public async Task ProcessFhirObservationAsync(string fhirJson)
    {
        // Implementação será adicionada posteriormente
        // Processar dados FHIR específicos
        await Task.CompletedTask;
    }

    public async Task RetryFailedDataAsync()
    {
        _logger.LogInformation("Retrying failed data...");
        
        var failedData = await _repository.GetDataByStatusAsync(IngestionStatus.Failed);
        
        foreach (var data in failedData)
        {
            data.Metadata.Status = IngestionStatus.Received;
            data.Metadata.ErrorMessage = null;
            await _repository.UpdateRawDataAsync(data);
        }
        
        await ProcessIncomingDataAsync();
    }
}
