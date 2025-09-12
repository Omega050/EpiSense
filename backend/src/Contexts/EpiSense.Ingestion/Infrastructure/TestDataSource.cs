using EpiSense.Ingestion.Domain;
using EpiSense.Ingestion.Domain.ValueObjects;
using Newtonsoft.Json;

namespace EpiSense.Ingestion.Infrastructure;

public class TestDataSource : IDataSource
{
    private readonly List<RawHealthData> _testData = new();

    public async Task<IEnumerable<RawHealthData>> GetPendingDataAsync()
    {
        // Para testes, retorna dados que ainda não foram processados
        return await Task.FromResult(_testData.Where(x => x.IngestionMetadata.Status == IngestionStatus.Received));
    }

    public async Task MarkAsProcessedAsync(string dataId)
    {
        var data = _testData.FirstOrDefault(x => x.Id == dataId);
        if (data != null)
        {
            data.IngestionMetadata.Status = IngestionStatus.Processed;
            data.IngestionMetadata.ProcessedAt = DateTime.UtcNow;
        }
        
        await Task.CompletedTask;
    }

    // Método para adicionar dados de teste
    public async Task AddTestDataAsync(string fhirJson)
    {
        var rawData = new RawHealthData
        {
            RawJson = fhirJson,
            IngestionMetadata = new IngestionMetadata
            {
                SourceSystem = "TestDataSource",
                SourceUrl = "http://localhost/test",
                Status = IngestionStatus.Received,
                ReceivedAt = DateTime.UtcNow,
                Version = "1.0"
            }
        };

        // Tenta fazer parse do JSON FHIR
        try
        {
            // Aqui posteriormente adicionaremos parser FHIR
            // Por enquanto, apenas armazena o JSON bruto
            rawData.IngestionMetadata.ValidationRules.Add("basic-json-structure");
        }
        catch (Exception ex)
        {
            rawData.IngestionMetadata.Status = IngestionStatus.ValidationFailed;
            rawData.IngestionMetadata.ErrorMessage = $"Invalid JSON: {ex.Message}";
        }

        _testData.Add(rawData);
        await Task.CompletedTask;
    }

    public async Task<RawHealthData?> GetTestDataByIdAsync(string id)
    {
        return await Task.FromResult(_testData.FirstOrDefault(x => x.Id == id));
    }

    public async Task<IEnumerable<RawHealthData>> GetAllTestDataAsync()
    {
        return await Task.FromResult(_testData.AsEnumerable());
    }
}