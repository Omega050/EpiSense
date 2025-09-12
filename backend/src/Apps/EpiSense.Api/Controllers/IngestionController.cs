using Microsoft.AspNetCore.Mvc;
using EpiSense.Ingestion.Services;
using EpiSense.Ingestion.Domain;
using EpiSense.Ingestion.Domain.ValueObjects;
using EpiSense.Ingestion.Infrastructure;

namespace EpiSense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionService _ingestionService;
    private readonly IIngestionRepository _repository;
    private readonly TestDataSource _testDataSource;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IngestionService ingestionService,
        IIngestionRepository repository,
        TestDataSource testDataSource,
        ILogger<IngestionController> logger)
    {
        _ingestionService = ingestionService;
        _repository = repository;
        _testDataSource = testDataSource;
        _logger = logger;
    }

    [HttpPost("test")]
    public async Task<IActionResult> IngestTestData([FromBody] FhirTestRequest request)
    {
        try
        {
            _logger.LogInformation("📥 Receiving FHIR data for ingestion test");

            // Adiciona os dados ao data source de teste
            await _testDataSource.AddTestDataAsync(request.FhirJson);

            // Cria objeto RawHealthData
            var rawData = new RawHealthData
            {
                RawJson = request.FhirJson,
                IngestionMetadata = new IngestionMetadata
                {
                    SourceSystem = "API-Test",
                    SourceUrl = Request.Path,
                    Status = IngestionStatus.Received,
                    ReceivedAt = DateTime.UtcNow
                }
            };

            // Salva no MongoDB
            await _repository.SaveRawDataAsync(rawData);

            _logger.LogInformation("✅ Data ingested successfully with ID: {DataId}", rawData.Id);

            return Ok(new
            {
                Success = true,
                DataId = rawData.Id,
                Status = rawData.IngestionMetadata.Status.ToString(),
                ReceivedAt = rawData.IngestionMetadata.ReceivedAt,
                Message = "Data successfully ingested for testing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during data ingestion test");
            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    [HttpGet("status/{dataId}")]
    public async Task<IActionResult> GetIngestionStatus(string dataId)
    {
        try
        {
            var data = await _repository.GetRawDataByIdAsync(dataId);
            
            if (data == null)
            {
                return NotFound(new { Error = $"Data with ID {dataId} not found" });
            }

            return Ok(new
            {
                DataId = data.Id,
                Status = data.IngestionMetadata.Status.ToString(),
                ReceivedAt = data.IngestionMetadata.ReceivedAt,
                ProcessedAt = data.IngestionMetadata.ProcessedAt,
                ErrorMessage = data.IngestionMetadata.ErrorMessage,
                RetryCount = data.IngestionMetadata.RetryCount,
                SourceSystem = data.IngestionMetadata.SourceSystem,
                HasFhirData = data.FhirObservation != null,
                RawDataSize = data.RawJson?.Length ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving ingestion status for {DataId}", dataId);
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetAllStatuses([FromQuery] string? status = null)
    {
        try
        {
            IEnumerable<RawHealthData> data;
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<IngestionStatus>(status, true, out var statusEnum))
            {
                data = await _repository.GetDataByStatusAsync(statusEnum);
            }
            else
            {
                // Para demonstração, pega dados dos últimos 7 dias
                var startDate = DateTime.UtcNow.AddDays(-7);
                data = await _repository.GetDataByDateRangeAsync(startDate, DateTime.UtcNow);
            }

            var result = data.Select(d => new
            {
                DataId = d.Id,
                Status = d.IngestionMetadata.Status.ToString(),
                ReceivedAt = d.IngestionMetadata.ReceivedAt,
                ProcessedAt = d.IngestionMetadata.ProcessedAt,
                SourceSystem = d.IngestionMetadata.SourceSystem,
                HasError = !string.IsNullOrEmpty(d.IngestionMetadata.ErrorMessage)
            });

            return Ok(new
            {
                TotalCount = result.Count(),
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving ingestion statuses");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPendingData()
    {
        try
        {
            _logger.LogInformation("🔄 Starting manual processing of pending data");
            
            await _ingestionService.ProcessIncomingDataAsync();
            
            return Ok(new
            {
                Success = true,
                Message = "Processing initiated for pending data"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during manual processing");
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

public class FhirTestRequest
{
    public string FhirJson { get; set; } = string.Empty;
}