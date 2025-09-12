using Microsoft.Extensions.Logging;

namespace EpiSense.Ingestion.Infrastructure;

public class ConsoleEventPublisher : IEventPublisher
{
    private readonly ILogger<ConsoleEventPublisher> _logger;

    public ConsoleEventPublisher(ILogger<ConsoleEventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishDataIngestedAsync(string dataId)
    {
        _logger.LogInformation("📥 Data Ingested: {DataId} at {Timestamp}", dataId, DateTime.UtcNow);
        
        // Simula publicação de evento
        await Task.Delay(10);
        
        Console.WriteLine($"✅ EVENT: Data {dataId} successfully ingested");
    }

    public async Task PublishDataValidationFailedAsync(string dataId, string error)
    {
        _logger.LogWarning("❌ Data Validation Failed: {DataId} - {Error} at {Timestamp}", dataId, error, DateTime.UtcNow);
        
        await Task.Delay(10);
        
        Console.WriteLine($"⚠️ EVENT: Data {dataId} validation failed - {error}");
    }

    public async Task PublishDataTransformedAsync(string dataId)
    {
        _logger.LogInformation("🔄 Data Transformed: {DataId} at {Timestamp}", dataId, DateTime.UtcNow);
        
        await Task.Delay(10);
        
        Console.WriteLine($"🔄 EVENT: Data {dataId} successfully transformed");
    }
}