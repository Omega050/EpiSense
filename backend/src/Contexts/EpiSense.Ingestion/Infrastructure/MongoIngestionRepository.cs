using MongoDB.Driver;
using EpiSense.Ingestion.Domain;
using Microsoft.Extensions.Options;

namespace EpiSense.Ingestion.Infrastructure;

public class MongoIngestionRepository : IIngestionRepository
{
    private readonly IMongoCollection<RawHealthData> _collection;
    private readonly MongoDbSettings _settings;

    public MongoIngestionRepository(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        var client = new MongoClient(_settings.ConnectionString);
        var database = client.GetDatabase(_settings.DatabaseName);
        _collection = database.GetCollection<RawHealthData>(_settings.RawHealthDataCollection);
    }

    public async Task SaveRawDataAsync(RawHealthData data)
    {
        await _collection.InsertOneAsync(data);
    }

    public async Task<RawHealthData?> GetRawDataByIdAsync(string id)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateStatusAsync(string id, IngestionStatus status, string? errorMessage = null)
    {
        var update = Builders<RawHealthData>.Update
            .Set(x => x.Status, status);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            update = update.Set(x => x.ErrorMessage, errorMessage);
        }

        await _collection.UpdateOneAsync(
            x => x.Id == id,
            update);
    }

    public async Task<IEnumerable<RawHealthData>> GetDataByStatusAsync(IngestionStatus status)
    {
        return await _collection
            .Find(x => x.Status == status)
            .ToListAsync();
    }

    public async Task<IEnumerable<RawHealthData>> GetDataByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _collection
            .Find(x => x.ReceivedAt >= startDate && x.ReceivedAt <= endDate)
            .ToListAsync();
    }
}