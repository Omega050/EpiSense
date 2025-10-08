using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EpiSense.Analysis.Infrastructure;

/// <summary>
/// Métodos de extensão para configurar os serviços de infraestrutura do módulo de análise
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adiciona o DbContext do módulo de análise
    /// </summary>
    public static IServiceCollection AddAnalysisInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AnalysisDatabase")
            ?? throw new InvalidOperationException("Connection string 'AnalysisDatabase' não encontrada.");

        services.AddDbContext<AnalysisDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "analysis");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });

            // Apenas em desenvolvimento
            var enableSensitiveDataLogging = configuration["EnableSensitiveDataLogging"];
            if (!string.IsNullOrEmpty(enableSensitiveDataLogging) && 
                bool.Parse(enableSensitiveDataLogging))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Garante que o banco de dados está criado e as migrations aplicadas
    /// </summary>
    public static async Task EnsureAnalysisDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        
        await context.Database.MigrateAsync();
    }
}
