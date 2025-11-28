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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnomalyController> _logger;

    public AnomalyController(
        IServiceScopeFactory scopeFactory,
        ILogger<AnomalyController> logger)
    {
        _scopeFactory = scopeFactory;
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

            using var scope = _scopeFactory.CreateScope();
            var shewhartJob = scope.ServiceProvider.GetRequiredService<ShewhartAnalysisJob>();
            var result = await shewhartJob.ExecuteForMunicipioAsync(municipioIbge, flag);
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
                using var scope = _scopeFactory.CreateScope();
                var shewhartJob = scope.ServiceProvider.GetRequiredService<ShewhartAnalysisJob>();
                await shewhartJob.ExecuteAsync();
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

    /// <summary>
    /// Força execução imediata do job de agregação para popular cache diário.
    /// Deve ser executado antes da análise Shewhart para garantir dados atualizados.
    /// </summary>
    /// <param name="days">Número de dias a agregar (padrão: 90)</param>
    /// <returns>Mensagem de confirmação</returns>
    [HttpPost("trigger-aggregation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerAggregation([FromQuery] int days = 90)
    {
        _logger.LogInformation("Execução manual do job de agregação solicitada via API para {Days} dias", days);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aggregationJob = scope.ServiceProvider.GetRequiredService<AggregationJob>();

                // Agrega dados para os últimos N dias
                for (int i = 2; i <= days; i++)
                {
                    var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                    _logger.LogInformation("Agregando D-{Offset}: {Date:yyyy-MM-dd}", i, targetDate);

                    var aggregationService = scope.ServiceProvider.GetRequiredService<Analysis.Services.AggregationService>();
                    await aggregationService.UpdateDailyAggregationsAsync(targetDate);
                }

                _logger.LogInformation("✅ Agregação completa para {Days} dias", days);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar agregação manual");
            }
        });

        return Accepted(new
        {
            message = $"Agregação iniciada em background para {days} dias",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Executa agregação + análise Shewhart em sequência.
    /// Útil para testes e validação do sistema completo.
    /// </summary>
    [HttpPost("trigger-full-analysis")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerFullAnalysis([FromQuery] int aggregationDays = 90)
    {
        _logger.LogInformation("Execução completa solicitada: Agregação + Shewhart");

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // 1. Agregação
                _logger.LogInformation("📊 Fase 1: Agregação de {Days} dias...", aggregationDays);
                var aggregationService = scope.ServiceProvider.GetRequiredService<Analysis.Services.AggregationService>();

                for (int i = 2; i <= aggregationDays; i++)
                {
                    var targetDate = DateTime.UtcNow.Date.AddDays(-i);
                    await aggregationService.UpdateDailyAggregationsAsync(targetDate);
                }
                _logger.LogInformation("✅ Agregação concluída");

                // 2. Análise Shewhart
                _logger.LogInformation("🔬 Fase 2: Análise Shewhart...");
                var shewhartJob = scope.ServiceProvider.GetRequiredService<ShewhartAnalysisJob>();
                await shewhartJob.ExecuteAsync();
                _logger.LogInformation("✅ Análise Shewhart concluída");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar análise completa");
            }
        });

        return Accepted(new
        {
            message = "Análise completa (Agregação + Shewhart) iniciada em background",
            aggregationDays,
            timestamp = DateTime.UtcNow
        });
    }
}
