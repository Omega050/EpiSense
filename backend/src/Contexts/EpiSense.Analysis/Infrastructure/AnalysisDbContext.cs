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
    public DbSet<AnalysisResult> AnalysisResults { get; set; } = null!;
    public DbSet<ObservationSummary> ObservationSummaries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da tabela AnalysisResult
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.ToTable("analysis_results", "analysis");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AnalysisType)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Region)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.AnalyzedAt)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.Notes)
                .HasMaxLength(2000);
            
            // Índices
            entity.HasIndex(e => e.AnalyzedAt);
            entity.HasIndex(e => e.Region);
            entity.HasIndex(e => e.AnalysisType);
        });

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
            
            entity.Property(e => e.RawDataId)
                .HasMaxLength(255);
            
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
            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.RawDataId);
        });
    }
}
