using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using EpiSense.Ingestion.Services;
using EpiSense.Ingestion.Infrastructure;
using EpiSense.Ingestion.Domain;
using EpiSense.Analysis.Services;
using EpiSense.Analysis.Infrastructure;
using EpiSense.Api.Jobs;
using MongoDB.Bson;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configuração MongoDB (Ingestion)
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

// Configuração PostgreSQL (Analysis) com suporte a JSON dinâmico
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("AnalysisDatabase"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseNpgsql(
        dataSource,
        npgsqlOptions => 
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        }));

// Serviços de Ingestão
builder.Services.AddScoped<IIngestionRepository, MongoIngestionRepository>();
builder.Services.AddScoped<IEventPublisher, ConsoleEventPublisher>();

// Serviços de Análise
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<FhirAnalysisService>();
builder.Services.AddScoped<AggregationService>();
builder.Services.AddScoped<ShewhartAnalyzer>();
builder.Services.AddScoped<AnalysisJob>();
builder.Services.AddScoped<AggregationJob>();
builder.Services.AddScoped<ShewhartAnalysisJob>();

// Configuração Hangfire para processamento assíncrono
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => 
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("AnalysisDatabase")))
);

// Adicionar servidor Hangfire (processa os jobs)
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Número de workers paralelos
    options.ServerName = "EpiSense-Analysis-Worker";
});

// Serviço principal de Ingestão (agora sem callback - análise será feita pelo Hangfire)
builder.Services.AddScoped<IngestionService>(serviceProvider =>
{
    var repository = serviceProvider.GetRequiredService<IIngestionRepository>();
    var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();
    var logger = serviceProvider.GetRequiredService<ILogger<IngestionService>>();

    // Sem callback - análise será enfileirada no Hangfire
    return new IngestionService(repository, eventPublisher, logger, null);
});

// Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EpiSense API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Hangfire Dashboard (acessível em /hangfire)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "EpiSense - Analysis Jobs",
    StatsPollingInterval = 2000 // Atualiza a cada 2 segundos
});

// Controllers
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Registrar jobs recorrentes no Hangfire
// 1️⃣ Agregação diária: D-2 (executa às 2h da manhã)
RecurringJob.AddOrUpdate<AggregationJob>(
    "agregacao-diaria",           // Nome do job
    job => job.ExecuteAsync(),    // Qual método executar
    Cron.Daily(2)                 // QUANDO: todo dia às 2h
);

// 2️⃣ Análise Shewhart: detecta anomalias epidemiológicas (executa a cada 2 horas)
RecurringJob.AddOrUpdate<ShewhartAnalysisJob>(
    "shewhart-analysis",          // Nome do job
    job => job.ExecuteAsync(),    // Qual método executar
    "0 */2 * * *"                 // QUANDO: a cada 2 horas (0min de horas pares: 00:00, 02:00, 04:00...)
);

app.Run();
