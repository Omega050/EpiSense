using Microsoft.AspNetCore.Mvc;
using EpiSense.Analysis.Infrastructure;

namespace EpiSense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisRepository _repository;

    public AnalysisController(IAnalysisRepository repository)
    {
        _repository = repository;
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
                o.ObservationId,
                DataColeta = o.DataColeta,
                CodigoMunicipioIBGE = o.CodigoMunicipioIBGE ?? "N/A",
                Flags = o.Flags,
                LabValues = o.LabValues,
                // Flags clínicas computadas
                HasSibSuspeita = o.HasSibSuspeita,
                HasSibGrave = o.HasSibGrave,
                HasAnySib = o.HasAnySib
            }).ToList();

            return Ok(new
            {
                Success = true,
                TotalCount = result.Count,
                Statistics = new
                {
                    WithFlags = result.Count(o => o.Flags.Count > 0),
                    WithoutFlags = result.Count(o => o.Flags.Count == 0),
                    SibSuspeitaCount = result.Count(o => o.HasSibSuspeita),
                    SibGraveCount = result.Count(o => o.HasSibGrave),
                    AnySibCount = result.Count(o => o.HasAnySib)
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
}
