using Microsoft.AspNetCore.Mvc;
using EpiSense.Ingestion.Services;
using EpiSense.Ingestion.Domain;
using EpiSense.Ingestion.Infrastructure;
using EpiSense.Api.Jobs;
using MongoDB.Bson;
using System.Text.Json;
using Hangfire;

namespace EpiSense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IngestionService _ingestionService;
    private readonly IIngestionRepository _repository;
    private readonly IBackgroundJobClient _backgroundJobs;

    public IngestionController(
        IngestionService ingestionService,
        IIngestionRepository repository,
        IBackgroundJobClient backgroundJobs)
    {
        _ingestionService = ingestionService;
        _repository = repository;
        _backgroundJobs = backgroundJobs;
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

            // 1. Salvar no MongoDB (ingestão rápida, sem callback)
            var rawData = await _ingestionService.IngestAndAnalyzeAsync(
                fhirJson: jsonString,
                sourceSystem: "API-Object",
                sourceUrl: Request.Path
            );

            // 2. Enfileirar análise assíncrona no Hangfire
            var fhirJsonForAnalysis = rawData.FhirData.ToJson();
            var jobId = _backgroundJobs.Enqueue<AnalysisJob>(job =>
                job.ProcessAnalysisAsync(
                    fhirJsonForAnalysis,
                    rawData.Id,
                    rawData.Metadata.ReceivedAt
                )
            );

            return Ok(new
            {
                Success = true,
                DataId = rawData.Id,
                JobId = jobId,
                Status = rawData.Metadata.Status.ToString(),
                ReceivedAt = rawData.Metadata.ReceivedAt,
                Message = "FHIR Observation ingested successfully. Analysis queued.",
                SourceSystem = rawData.Metadata.SourceSystem
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
                Status = data.Metadata.Status.ToString(),
                ReceivedAt = data.Metadata.ReceivedAt,
                ErrorMessage = data.Metadata.ErrorMessage,
                SourceSystem = data.Metadata.SourceSystem,
                SourceUrl = data.Metadata.SourceUrl,
                RawDataSize = data.FhirData?.ElementCount ?? 0
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
                Status = d.Metadata.Status.ToString(),
                ReceivedAt = d.Metadata.ReceivedAt,
                SourceSystem = d.Metadata.SourceSystem,
                HasError = !string.IsNullOrEmpty(d.Metadata.ErrorMessage)
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
