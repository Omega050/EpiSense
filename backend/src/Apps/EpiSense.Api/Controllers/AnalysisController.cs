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
                ProcessedAt = o.ProcessedAt,
                o.RawDataId,
                // Informações agregadas
                FlagCount = o.Flags.Count,
                HasClinicalFlags = o.Flags.Count > 0,
                // Flags específicas
                HasDengue = o.Flags.Contains("DENGUE"),
                HasAnemia = o.Flags.Contains("ANEMIA"),
                HasTrombocitopenia = o.Flags.Contains("TROMBOCITOPENIA"),
                HasLeucopenia = o.Flags.Contains("LEUCOPENIA_INTENSA") || o.Flags.Contains("LEUCOPENIA_MODERADA"),
                HasHemoconcentracao = o.Flags.Contains("HEMOCONCENTRACAO"),
                HasHemoglobinaBaixa = o.Flags.Contains("HEMOGLOBINA_BAIXA"),
                HasMicrocitose = o.Flags.Contains("MICROCITOSE"),
                HasAnisocitose = o.Flags.Contains("ANISOCITOSE")
            }).ToList();

            return Ok(new
            {
                Success = true,
                TotalCount = result.Count,
                Statistics = new
                {
                    WithFlags = result.Count(o => o.HasClinicalFlags),
                    WithoutFlags = result.Count(o => !o.HasClinicalFlags),
                    DengueFlags = result.Count(o => o.HasDengue),
                    AnemiaFlags = result.Count(o => o.HasAnemia),
                    TrombocitopeniaFlags = result.Count(o => o.HasTrombocitopenia),
                    LeucopeniaFlags = result.Count(o => o.HasLeucopenia),
                    HemoconcentracaoFlags = result.Count(o => o.HasHemoconcentracao)
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
