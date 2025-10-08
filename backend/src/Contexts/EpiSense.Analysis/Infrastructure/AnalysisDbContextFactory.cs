using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EpiSense.Analysis.Infrastructure;

/// <summary>
/// Factory para criação do DbContext em tempo de design (migrations, etc)
/// </summary>
public class AnalysisDbContextFactory : IDesignTimeDbContextFactory<AnalysisDbContext>
{
    public AnalysisDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalysisDbContext>();
        
        // Connection string para desenvolvimento/migrations
        // Em produção, isso virá do appsettings.json
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=episense_analysis;Username=episense;Password=episense_dev_pass",
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "analysis")
        );

        return new AnalysisDbContext(optionsBuilder.Options);
    }
}
