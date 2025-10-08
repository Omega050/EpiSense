using EpiSense.Analysis.TestData;

// Executar os testes de conexão e operações básicas
Console.WriteLine("=================================================");
Console.WriteLine("🧪 Testando PostgreSQL - Módulo de Análise");
Console.WriteLine("=================================================\n");

try
{
    await DatabaseConnectionTest.TestConnectionAndOperations();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Erro durante os testes: {ex.Message}");
    Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
    return 1;
}

return 0;
