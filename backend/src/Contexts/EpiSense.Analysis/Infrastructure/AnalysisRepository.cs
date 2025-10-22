using EpiSense.Analysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EpiSense.Analysis.Infrastructure;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AnalysisDbContext _context;

    public AnalysisRepository(AnalysisDbContext context)
    {
        _context = context;
    }

    public async Task<ObservationSummary> SaveAsync(ObservationSummary summary)
    {
        await _context.ObservationSummaries.AddAsync(summary);
        await _context.SaveChangesAsync();
        return summary;
    }

    public async Task<ObservationSummary?> GetByIdAsync(Guid id)
    {
        return await _context.ObservationSummaries
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<ObservationSummary>> GetAllAsync()
    {
        return await _context.ObservationSummaries
            .OrderByDescending(s => s.DataColeta)
            .ToListAsync();
    }

    public async Task<IEnumerable<ObservationSummary>> GetByMunicipioAsync(
        string codigoIbge, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _context.ObservationSummaries
            .Where(s => s.CodigoMunicipioIBGE == codigoIbge);

        if (startDate.HasValue)
            query = query.Where(s => s.DataColeta >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.DataColeta <= endDate.Value);

        return await query
            .OrderByDescending(s => s.DataColeta)
            .ToListAsync();
    }

    public async Task<IEnumerable<ObservationSummary>> GetByFlagsAsync(
        string[] flags, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _context.ObservationSummaries.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.DataColeta >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.DataColeta <= endDate.Value);

        var results = await query.ToListAsync();
    
        return results
            .Where(s => s.Flags.Any(f => flags.Contains(f)))
            .OrderByDescending(s => s.DataColeta)
            .ToList();
    }

    public async Task<IEnumerable<ObservationSummary>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.ObservationSummaries
            .Where(s => s.DataColeta >= startDate && s.DataColeta <= endDate)
            .OrderByDescending(s => s.DataColeta)
            .ToListAsync();
    }

    public async Task<int> CountByMunicipioAndFlagsAsync(
        string codigoIbge, 
        string[] flags, 
        DateTime startDate, 
        DateTime endDate)
    {
        var summaries = await _context.ObservationSummaries
            .Where(s => s.CodigoMunicipioIBGE == codigoIbge 
                     && s.DataColeta >= startDate 
                     && s.DataColeta <= endDate)
            .ToListAsync();

        return summaries.Count(s => s.Flags.Any(f => flags.Contains(f)));
    }
}
