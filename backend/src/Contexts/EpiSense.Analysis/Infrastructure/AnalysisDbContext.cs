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
    }
}
