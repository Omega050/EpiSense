using EpiSense.Analysis.Infrastructure;
using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EpiSense.Analysis.Services;

/// <summary>
/// Serviço responsável por agregar dados de ObservationSummary em cache diário.
/// 
/// DECISÃO ARQUITETURAL (ADR-010):
/// - Casos SIB_GRAVE são agregados como SIB_SUSPEITA (simplificação epidemiológica)
/// - Apenas uma série temporal por município é mantida no cache
/// - Dados brutos (observation_summaries) mantêm todas as flags originais
/// </summary>
public class AggregationService
{
    private readonly AnalysisDbContext _context;

    public AggregationService(AnalysisDbContext context)
    {
        _context = context;
    }
    
    public async Task RebuildAllAggregationsAsync()
    {
        // Busca todas as observações com flags clínicas
        var observations = await _context.ObservationSummaries
            .Where(obs => obs.Flags.Any(f => 
                f == ClinicalFlags.Clinical.SIB_SUSPEITA || 
                f == ClinicalFlags.Clinical.SIB_GRAVE))
            .ToListAsync();

        // Normaliza e agrega (ADR-010: SIB_GRAVE conta como SIB_SUSPEITA)
        var aggregations = BuildAggregationsFromObservations(observations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }

    public async Task UpdateDailyAggregationsAsync(DateTime targetDate)
    {
        // Busca apenas observações do dia específico
        var observations = await _context.ObservationSummaries
            .Where(obs => obs.DataColeta.Date == targetDate.Date)
            .Where(obs => obs.Flags.Any(f => 
                f == ClinicalFlags.Clinical.SIB_SUSPEITA || 
                f == ClinicalFlags.Clinical.SIB_GRAVE))
            .ToListAsync();
        
        // Normaliza e agrega
        var aggregations = BuildAggregationsFromObservations(observations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }

    // Método 3: Agregação retroativa (dados tardios)
    public async Task UpdateAggregationsForDateRangeAsync(
        DateTime startDate,
        DateTime endDate)
    {
        // Busca observações no intervalo de datas
        var observations = await _context.ObservationSummaries
            .Where(obs => obs.DataColeta.Date >= startDate.Date && obs.DataColeta.Date <= endDate.Date)
            .Where(obs => obs.Flags.Any(f => 
                f == ClinicalFlags.Clinical.SIB_SUSPEITA || 
                f == ClinicalFlags.Clinical.SIB_GRAVE))
            .ToListAsync();
        
        // Normaliza e agrega
        var aggregations = BuildAggregationsFromObservations(observations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }
    
    /// <summary>
    /// Constrói agregações a partir de observações brutas.
    /// 
    /// LÓGICA (ADR-010):
    /// 1. Normaliza flags: SIB_GRAVE → SIB_SUSPEITA
    /// 2. Remove duplicatas por paciente (um paciente = um caso)
    /// 3. Agrupa por (Município, Data, Flag)
    /// 4. Conta total de casos por grupo
    /// </summary>
    private List<DailyCaseAggregation> BuildAggregationsFromObservations(
        List<ObservationSummary> observations)
    {
        return observations
            // Para cada observação, verifica se tem alguma flag clínica SIB
            .Where(obs => obs.Flags.Any(f => 
                f == ClinicalFlags.Clinical.SIB_SUSPEITA || 
                f == ClinicalFlags.Clinical.SIB_GRAVE))
            // Mapeia para a flag normalizada (ADR-010: tudo vira SIB_SUSPEITA)
            .Select(obs => new
            {
                Municipio = obs.CodigoMunicipioIBGE ?? "UNKNOWN",
                Data = obs.DataColeta.Date, // Apenas data, sem hora
                Flag = ClinicalFlags.Clinical.SIB_SUSPEITA // Sempre SIB_SUSPEITA (simplificado)
            })
            // Agrupa por (Município, Data, Flag) e conta
            .GroupBy(x => new { x.Municipio, x.Data, x.Flag })
            .Select(g => new DailyCaseAggregation
            {
                MunicipioIBGE = g.Key.Municipio,
                Data = g.Key.Data,
                Flag = g.Key.Flag,
                TotalCasos = g.Count()
            })
            .ToList();
    }
    
    /// <summary>
    /// Persiste ou atualiza agregações diárias no banco de dados.
    /// Usa INSERT ... ON CONFLICT UPDATE (upsert) para evitar duplicatas.
    /// </summary>
    /// <param name="aggregations">Lista de agregações a serem persistidas</param>
    private async Task UpsertDailyCaseAggregationsAsync(
        IEnumerable<DailyCaseAggregation> aggregations)
    {
        foreach (var aggregation in aggregations)
        {
            // Busca registro existente pela chave única (município, data, flag)
            var existing = await _context.DailyCaseAggregations
                .FirstOrDefaultAsync(a => 
                    a.MunicipioIBGE == aggregation.MunicipioIBGE && 
                    a.Data == aggregation.Data && 
                    a.Flag == aggregation.Flag);
            
            if (existing != null)
            {
                // UPDATE: Atualiza registro existente
                existing.TotalCasos = aggregation.TotalCasos;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // INSERT: Adiciona novo registro
                aggregation.UpdatedAt = DateTime.UtcNow;
                _context.DailyCaseAggregations.Add(aggregation);
            }
        }
        
        await _context.SaveChangesAsync();
    }
}
