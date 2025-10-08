using EpiSense.Analysis.Domain.Entities;
using EpiSense.Analysis.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EpiSense.Analysis.TestData;

/// <summary>
/// Classe para testar a conexão e operações básicas com o PostgreSQL
/// </summary>
public class DatabaseConnectionTest
{
    public static async Task TestConnectionAndOperations()
    {
        // Configurar o DbContext
        var optionsBuilder = new DbContextOptionsBuilder<AnalysisDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=episense_analysis;Username=episense;Password=episense_dev_pass"
        );

        await using var context = new AnalysisDbContext(optionsBuilder.Options);

        // Teste 1: Verificar conexão
        Console.WriteLine("🔍 Teste 1: Verificando conexão com o banco...");
        var canConnect = await context.Database.CanConnectAsync();
        Console.WriteLine($"✅ Conexão estabelecida: {canConnect}");

        if (!canConnect)
        {
            Console.WriteLine("❌ Não foi possível conectar ao banco de dados!");
            return;
        }

        // Teste 2: Inserir dados de teste
        Console.WriteLine("\n📝 Teste 2: Inserindo dados de teste...");
        var testResult = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisType = "Outbreak Detection",
            Region = "São Paulo",
            AnalyzedAt = DateTime.UtcNow,
            CasesCount = 150,
            RiskScore = 7.5,
            Notes = "Teste de inserção de dados via EF Core",
            CreatedAt = DateTime.UtcNow
        };

        context.AnalysisResults.Add(testResult);
        await context.SaveChangesAsync();
        Console.WriteLine($"✅ Registro inserido com ID: {testResult.Id}");

        // Teste 3: Consultar dados
        Console.WriteLine("\n🔎 Teste 3: Consultando dados...");
        var results = await context.AnalysisResults
            .Where(r => r.Region == "São Paulo")
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();

        Console.WriteLine($"✅ Encontrados {results.Count} registro(s):");
        foreach (var result in results)
        {
            Console.WriteLine($"   - ID: {result.Id}");
            Console.WriteLine($"     Tipo: {result.AnalysisType}");
            Console.WriteLine($"     Região: {result.Region}");
            Console.WriteLine($"     Casos: {result.CasesCount}");
            Console.WriteLine($"     Score de Risco: {result.RiskScore}");
            Console.WriteLine($"     Analisado em: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}");
        }

        // Teste 4: Atualizar dados
        Console.WriteLine("\n✏️ Teste 4: Atualizando dados...");
        var recordToUpdate = await context.AnalysisResults.FirstOrDefaultAsync(r => r.Id == testResult.Id);
        if (recordToUpdate != null)
        {
            recordToUpdate.CasesCount = 175;
            recordToUpdate.RiskScore = 8.2;
            recordToUpdate.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Registro atualizado: Casos={recordToUpdate.CasesCount}, Score={recordToUpdate.RiskScore}");
        }

        // Teste 5: Contar registros
        Console.WriteLine("\n📊 Teste 5: Estatísticas...");
        var totalRecords = await context.AnalysisResults.CountAsync();
        var avgRiskScore = await context.AnalysisResults.AverageAsync(r => r.RiskScore);
        var totalCases = await context.AnalysisResults.SumAsync(r => r.CasesCount);
        
        Console.WriteLine($"✅ Total de registros: {totalRecords}");
        Console.WriteLine($"✅ Score médio de risco: {avgRiskScore:F2}");
        Console.WriteLine($"✅ Total de casos: {totalCases}");

        // Teste 6: Limpar dados de teste
        Console.WriteLine("\n🧹 Teste 6: Limpando dados de teste...");
        context.AnalysisResults.RemoveRange(context.AnalysisResults);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Dados de teste removidos");

        Console.WriteLine("\n🎉 Todos os testes concluídos com sucesso!");
    }
}
