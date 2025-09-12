namespace EpiSense.Ingestion.Infrastructure;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string RawHealthDataCollection { get; set; } = "raw_health_data";
    public int MaxConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
