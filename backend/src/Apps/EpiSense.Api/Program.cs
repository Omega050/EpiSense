using Microsoft.OpenApi.Models;
using EpiSense.Ingestion.Services;
using EpiSense.Ingestion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configuração MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

// Serviços de Ingestão
builder.Services.AddScoped<IIngestionRepository, MongoIngestionRepository>();
builder.Services.AddScoped<IEventPublisher, ConsoleEventPublisher>();
builder.Services.AddScoped<IngestionService>();

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

// Controllers
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
