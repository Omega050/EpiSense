using Microsoft.AspNetCore.Mvc;
using EpiSense.Ingestion.Services;
using EpiSense.Ingestion.Domain;
using EpiSense.Ingestion.Infrastructure;
using System.Text.Json;

namespace EpiSense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionService _ingestionService;
    private readonly IIngestionRepository _repository;

    public IngestionController(
        IngestionService ingestionService,
        IIngestionRepository repository)
    {
        _ingestionService = ingestionService;
        _repository = repository;
    }

    [HttpPost("test")]
    public async Task<IActionResult> IngestTestData([FromBody] FhirTestRequest request)
    {
        try
        {
            // Validação básica do JSON
            if (string.IsNullOrWhiteSpace(request.FhirJson))
            {
                return BadRequest(new
                {
                    Success = false,
                    Error = "FhirJson cannot be empty"
                });
            }

            // Cria objeto RawHealthData simplificado
            var rawData = new RawHealthData
            {
                RawJson = request.FhirJson,
                SourceSystem = "API-Test",
                SourceUrl = Request.Path,
                Status = IngestionStatus.Received,
                ReceivedAt = DateTime.UtcNow
            };

            // Salva no MongoDB
            await _repository.SaveRawDataAsync(rawData);

            return Ok(new
            {
                Success = true,
                DataId = rawData.Id,
                Status = rawData.Status.ToString(),
                ReceivedAt = rawData.ReceivedAt,
                Message = "Data successfully ingested",
                SourceSystem = rawData.SourceSystem
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    [HttpPost("observation")]
    public async Task<IActionResult> IngestFhirObservation([FromBody] JsonElement observationJson)
    {
        try
        {
            // Converte o JsonElement para string
            var jsonString = observationJson.GetRawText();

            // Validação básica do JSON
            if (string.IsNullOrWhiteSpace(jsonString) || jsonString == "null")
            {
                return BadRequest(new
                {
                    Success = false,
                    Error = "Observation JSON cannot be empty"
                });
            }

            // Cria objeto RawHealthData simplificado
            var rawData = new RawHealthData
            {
                RawJson = jsonString,
                SourceSystem = "API-Object",
                SourceUrl = Request.Path,
                Status = IngestionStatus.Received,
                ReceivedAt = DateTime.UtcNow
            };

            // Salva no MongoDB
            await _repository.SaveRawDataAsync(rawData);

            return Ok(new
            {
                Success = true,
                DataId = rawData.Id,
                Status = rawData.Status.ToString(),
                ReceivedAt = rawData.ReceivedAt,
                Message = "FHIR Observation successfully ingested (Object format)",
                SourceSystem = rawData.SourceSystem
            });
        }
        catch (Exception ex)
        {
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
                Status = data.Status.ToString(),
                ReceivedAt = data.ReceivedAt,
                ErrorMessage = data.ErrorMessage,
                SourceSystem = data.SourceSystem,
                SourceUrl = data.SourceUrl,
                RawDataSize = data.RawJson?.Length ?? 0
            });
        }
        catch (Exception ex)
        {
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
                Status = d.Status.ToString(),
                ReceivedAt = d.ReceivedAt,
                SourceSystem = d.SourceSystem,
                HasError = !string.IsNullOrEmpty(d.ErrorMessage)
            });

            return Ok(new
            {
                TotalCount = result.Count(),
                Data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPendingData()
    {
        try
        {
            
            await _ingestionService.ProcessIncomingDataAsync();
            
            return Ok(new
            {
                Success = true,
                Message = "Processing initiated for pending data"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

public class FhirTestRequest
{
    public string FhirJson { get; set; } = string.Empty;
}
