using Microsoft.AspNetCore.Mvc;
using EpiSense.Analysis.Infrastructure;
using EpiSense.Analysis.Services;
namespace EpiSense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisRepository _repository;
    private readonly AggregationService _aggregationService;

    public AnalysisController(IAnalysisRepository repository, AggregationService aggregationService)
    {
        _repository = repository;
        _aggregationService = aggregationService;
    }


    /// <summary>
    /// Retorna todas as observações analisadas com suas flags clínicas
    /// GET /api/Analysis/observations
    /// </summary>
    [HttpGet("observations")]
    public async Task<IActionResult> GetAllAnalyzedObservations()
    {
        try
        {
            var observations = await _repository.GetAllAsync();
            var observationsList = observations.ToList();

            var result = observationsList.Select(o => new
            {
                o.Id,
                DataColeta = o.DataColeta,
                CodigoMunicipioIBGE = o.CodigoMunicipioIBGE ?? "N/A",
                Flags = o.Flags,
                LabValues = o.LabValues,
                // Flags clínicas computadas
                HasSibSuspeita = o.HasSibSuspeita,
                HasSibGrave = o.HasSibGrave
            }).ToList();

            return Ok(new
            {
                Success = true,
                TotalCount = result.Count,
                Statistics = new
                {
                    SibSuspeitaCount = result.Count(o => o.HasSibSuspeita),
                    SibGraveCount = result.Count(o => o.HasSibGrave)
                },
                Data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Error = ex.Message,
                Details = ex.InnerException?.Message
            });
        }
    }
    [HttpPost("aggregations/rebuild")]
    public async Task<IActionResult> RebuildAggregations()
    {
        await _aggregationService.RebuildAllAggregationsAsync();
        return Accepted(new { message = "Reconstrução iniciada" });
    }
}
