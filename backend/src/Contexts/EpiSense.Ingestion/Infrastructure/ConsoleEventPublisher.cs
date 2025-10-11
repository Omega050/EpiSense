namespace EpiSense.Ingestion.Infrastructure;

public class ConsoleEventPublisher : IEventPublisher
{
    public async Task PublishDataIngestedAsync(string dataId)
    {
        // Simula publicação de evento
        await Task.Delay(10);
        
        Console.WriteLine($"✅ EVENT: Data {dataId} successfully ingested");
    }

    public async Task PublishDataValidationFailedAsync(string dataId, string error)
    {
        await Task.Delay(10);
        
        Console.WriteLine($"⚠️ EVENT: Data {dataId} validation failed - {error}");
    }

    public async Task PublishDataTransformedAsync(string dataId)
    {
        await Task.Delay(10);
        
        Console.WriteLine($"🔄 EVENT: Data {dataId} successfully transformed");
    }
}