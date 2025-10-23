using Microsoft.EntityFrameworkCore;
using EpiSense.Analysis.Domain.Entities;

namespace EpiSense.Analysis.Infrastructure;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<ObservationSummary> ObservationSummaries { get; set; } = null!;
    public DbSet<DailyCaseAggregation> DailyCaseAggregations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da tabela ObservationSummary
        modelBuilder.Entity<ObservationSummary>(entity =>
        {
            entity.ToTable("observation_summaries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ObservationId)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.CodigoMunicipioIBGE)
                .HasMaxLength(7);
            
            // Configuração JSONB para PostgreSQL
            entity.Property(e => e.Flags)
                .HasColumnType("jsonb")
                .IsRequired();
            
            entity.Property(e => e.LabValues)
                .HasColumnType("jsonb")
                .IsRequired();
            
            // Índices para otimizar consultas epidemiológicas
            entity.HasIndex(e => e.DataColeta);
            entity.HasIndex(e => e.CodigoMunicipioIBGE);
            
            // Índice GIN para buscar por flags específicas (PostgreSQL JSONB)
            entity.HasIndex(e => e.Flags)
                .HasMethod("gin");
        });

        // Configuração da tabela DailyCaseAggregation
        modelBuilder.Entity<DailyCaseAggregation>(entity =>
        {
            entity.ToTable("daily_case_aggregations");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.MunicipioIBGE)
                .IsRequired()
                .HasMaxLength(7);
            
            entity.Property(e => e.Data)
                .IsRequired()
                .HasColumnType("date");
            
            entity.Property(e => e.Flag)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.TotalCasos)
                .IsRequired()
                .HasDefaultValue(0);
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("NOW()");
            
            // Constraint única: apenas 1 registro por (município, data, flag)
            entity.HasIndex(e => new { e.MunicipioIBGE, e.Data, e.Flag })
                .IsUnique()
                .HasDatabaseName("IX_daily_case_aggregations_unique");
            
            // Índice de lookup otimizado para consultas do Shewhart
            entity.HasIndex(e => new { e.MunicipioIBGE, e.Data, e.Flag })
                .HasDatabaseName("IX_daily_case_aggregations_lookup");
        });
    }
}
