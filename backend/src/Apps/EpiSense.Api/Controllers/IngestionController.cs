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
            // Usa o JSON original (jsonString) em vez de rawData.FhirData.ToJson()
            // para evitar metadados extras do MongoDB
            var jobId = _backgroundJobs.Enqueue<AnalysisJob>(job =>
                job.ProcessAnalysisAsync(
                    jsonString,  // JSON FHIR original
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
}
