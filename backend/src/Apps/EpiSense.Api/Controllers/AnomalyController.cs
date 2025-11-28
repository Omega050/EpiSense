using Microsoft.AspNetCore.Mvc;
using EpiSense.Api.Jobs;
using EpiSense.Analysis.Domain.ValueObjects;

namespace EpiSense.Api.Controllers;

/// <summary>
/// Endpoints para análise de anomalias epidemiológicas (Shewhart)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnomalyController : ControllerBase
{
    private readonly ShewhartAnalysisJob _shewhartJob;
    private readonly ILogger<AnomalyController> _logger;

    public AnomalyController(
        ShewhartAnalysisJob shewhartJob,
        ILogger<AnomalyController> logger)
    {
        _shewhartJob = shewhartJob;
        _logger = logger;
    }

    /// <summary>
    /// Executa análise Shewhart manual para um município e flag específicos.
    /// Útil para investigações ad-hoc ou validação de alertas.
    /// </summary>
    /// <param name="municipioIbge">Código IBGE do município (7 dígitos)</param>
    /// <param name="flag">Flag clínica (ex: SIB_SUSPEITA, LEUCOCITOSE)</param>
    /// <returns>Resultado da análise Shewhart</returns>
    /// <response code="200">Análise executada com sucesso</response>
    /// <response code="400">Parâmetros inválidos</response>
    [HttpGet("analyze/{municipioIbge}/{flag}")]
    [ProducesResponseType(typeof(ShewhartResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShewhartResult>> AnalyzeMunicipio(
        string municipioIbge,
        string flag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(municipioIbge) || municipioIbge.Length != 7)
                return BadRequest("Código IBGE deve ter 7 dígitos");

            if (string.IsNullOrWhiteSpace(flag))
                return BadRequest("Flag clínica é obrigatória");

            var result = await _shewhartJob.ExecuteForMunicipioAsync(municipioIbge, flag);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Erro ao executar análise manual: Município={MunicipioIbge}, Flag={Flag}",
                municipioIbge, flag);
            
            return StatusCode(500, new { error = "Erro ao executar análise", details = ex.Message });
        }
    }

    /// <summary>
    /// Força execução imediata do job de análise Shewhart para todos os municípios.
    /// Use com cautela - pode levar vários minutos dependendo do volume de dados.
    /// </summary>
    /// <returns>Mensagem de confirmação</returns>
    /// <response code="202">Job iniciado com sucesso</response>
    [HttpPost("trigger-analysis")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerAnalysis()
    {
        _logger.LogInformation("Execução manual do job Shewhart solicitada via API");
        
        // Executa em background task para não bloquear resposta HTTP
        _ = Task.Run(async () =>
        {
            try
            {
                await _shewhartJob.ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar job Shewhart manual");
            }
        });

        return Accepted(new 
        { 
            message = "Análise Shewhart iniciada em background",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Retorna informações sobre as flags monitoradas e configuração do algoritmo.
    /// </summary>
    [HttpGet("config")]
    public ActionResult<object> GetConfig()
    {
        return Ok(new
        {
            flags = new[]
            {
                "SIB_SUSPEITA",
                "SIB_GRAVE",
                "LEUCOCITOSE",
                "NEUTROFILIA",
                "DESVIO_ESQUERDA"
            },
            algorithm = new
            {
                name = "Shewhart Control Chart",
                baselineDays = 60,
                controlLimitSigma = 3.0,
                minimumCasesForAnalysis = 90,
                targetDateOffset = -2, // D-2 (dados consolidados)
                executionInterval = "Every 2 hours"
            },
            temporalStrategy = new
            {
                freshDataWindow = "D-0, D-1 (individual analysis)",
                consolidatedData = "D-2 (Shewhart target)",
                historicalBaseline = "D-3 to D-62 (60 days)"
            }
        });
    }
}
