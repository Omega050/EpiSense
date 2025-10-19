using EpiSense.Ingestion.Domain;

namespace EpiSense.Ingestion.Infrastructure;

public interface IDataSource
{
    Task<IEnumerable<RawHealthData>> GetPendingDataAsync();
    Task MarkAsProcessedAsync(string dataId);
}

public interface IIngestionRepository
{
    Task SaveRawDataAsync(RawHealthData data);
    Task<RawHealthData?> GetRawDataByIdAsync(string id);
    Task UpdateStatusAsync(string id, IngestionStatus status, string? errorMessage = null);
    Task UpdateRawDataAsync(RawHealthData data);
    Task<IEnumerable<RawHealthData>> GetDataByStatusAsync(IngestionStatus status);
    Task<IEnumerable<RawHealthData>> GetDataByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public interface IEventPublisher
{
    Task PublishDataIngestedAsync(string dataId);
    Task PublishDataValidationFailedAsync(string dataId, string error);
    Task PublishDataTransformedAsync(string dataId);
}
