using EpiSense.Analysis.Infrastructure;
using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EpiSense.Analysis.Services;

/// <summary>
/// Serviço responsável por agregar dados de ObservationSummary em cache diário.
/// 
/// DECISÃO ARQUITETURAL (ADR-011):
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
        // Busca TODAS as observações e filtra em memória
        var observations = await _context.ObservationSummaries
            .ToListAsync();

        // Filtra apenas observações com SIB (suspeita ou grave)
        var sibObservations = observations
            .Where(obs => obs.HasSibSuspeita || obs.HasSibGrave)
            .ToList();

        // BuildAggregationsFromObservations normaliza e agrega
        var aggregations = BuildAggregationsFromObservations(sibObservations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }

    public async Task UpdateDailyAggregationsAsync(DateTime targetDate)
    {
        // Busca observações do dia (filtro de data funciona no SQL)
        var observations = await _context.ObservationSummaries
            .Where(obs => obs.DataColeta.Date == targetDate.Date)
            .ToListAsync();
        
        // Filtra apenas observações com SIB (suspeita ou grave)
        var sibObservations = observations
            .Where(obs => obs.HasSibSuspeita || obs.HasSibGrave)
            .ToList();
        
        // BuildAggregationsFromObservations normaliza e agrega
        var aggregations = BuildAggregationsFromObservations(sibObservations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }

    // Método 3: Agregação retroativa (dados tardios)
    public async Task UpdateAggregationsForDateRangeAsync(
        DateTime startDate,
        DateTime endDate)
    {
        // Busca observações no intervalo (filtro de data funciona no SQL)
        var observations = await _context.ObservationSummaries
            .Where(obs => obs.DataColeta.Date >= startDate.Date && obs.DataColeta.Date <= endDate.Date)
            .ToListAsync();
        
        // Filtra apenas observações com SIB (suspeita ou grave)
        var sibObservations = observations
            .Where(obs => obs.HasSibSuspeita || obs.HasSibGrave)
            .ToList();
        
        // BuildAggregationsFromObservations normaliza e agrega
        var aggregations = BuildAggregationsFromObservations(sibObservations);
        
        // Persiste no banco
        await UpsertDailyCaseAggregationsAsync(aggregations);
    }
    
    /// <summary>
    /// Constrói agregações a partir de observações brutas.
    /// 
    /// LÓGICA (ADR-011 + Peso para Casos Graves):
    /// 1. Identifica casos SIB (suspeita ou grave)
    /// 2. Casos GRAVES contam com PESO 2 (duplica o registro)
    /// 3. Casos SUSPEITA contam com PESO 1 (registro único)
    /// 4. Normaliza todas as flags para SIB_SUSPEITA
    /// 5. Agrupa por (Município, Data, Flag) e conta
    /// </summary>
    private List<DailyCaseAggregation> BuildAggregationsFromObservations(
        List<ObservationSummary> observations)
    {
        // Expande casos graves para contarem 2x
        var weightedObservations = observations
            .SelectMany(obs => 
            {
                // Se for caso GRAVE, duplica (peso 2)
                if (obs.HasSibGrave)
                    return new[] { obs, obs };
                
                // Se for apenas SUSPEITA, mantém único (peso 1)
                return new[] { obs };
            });
        
        return weightedObservations
            .Select(obs => new
            {
                Municipio = obs.CodigoMunicipioIBGE ?? "UNKNOWN",
                Data = obs.DataColeta.Date, // Apenas data, sem hora
                Flag = ClinicalFlags.Clinical.SIB_SUSPEITA // Sempre SIB_SUSPEITA (ADR-011)
            })
            // Agrupa por (Município, Data, Flag) e conta
            .GroupBy(x => new { x.Municipio, x.Data, x.Flag })
            .Select(g => new DailyCaseAggregation
            {
                MunicipioIBGE = g.Key.Municipio,
                Data = g.Key.Data,
                Flag = g.Key.Flag,
                TotalCasos = g.Count() // Casos graves contam 2x
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
