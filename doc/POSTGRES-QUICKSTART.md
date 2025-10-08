# Guia Rápido - PostgreSQL no EpiSense Analysis

## 🚀 Iniciar o PostgreSQL

```powershell
# Iniciar o PostgreSQL
docker-compose up -d postgres

# Verificar se está rodando
docker-compose ps postgres
```

## 📦 Restaurar os pacotes NuGet

```powershell
cd backend\src\Contexts\EpiSense.Analysis
dotnet restore
```

## 🔧 Registrar o DbContext na API

No arquivo `Program.cs` da API, adicione:

```csharp
using EpiSense.Analysis.Infrastructure;

// Adicionar o DbContext
builder.Services.AddAnalysisInfrastructure(builder.Configuration);

// Após app.Build(), aplicar migrations automaticamente
await app.Services.EnsureAnalysisDatabaseAsync();
```

## 🗄️ Criar e Aplicar Migrations

```powershell
# Navegar até o projeto
cd backend\src\Contexts\EpiSense.Analysis

# Criar uma nova migration
dotnet ef migrations add InitialCreate

# Aplicar a migration no banco
dotnet ef database update

# Listar migrations
dotnet ef migrations list
```

## 📝 Adicionar Entidades ao DbContext

Edite `Infrastructure/AnalysisDbContext.cs`:

```csharp
public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options)
    {
    }

    // Seus DbSets
    public DbSet<AnalysisResult> AnalysisResults { get; set; }
    public DbSet<Outbreak> Outbreaks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações das entidades
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.ToTable("analysis_results", "analysis");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Outbreak>(entity =>
        {
            entity.ToTable("outbreaks", "analysis");
            entity.HasKey(e => e.Id);
        });
    }
}
```

## 🔌 Conectar ao PostgreSQL via linha de comando

```powershell
# Conectar ao PostgreSQL
docker exec -it episense-postgres psql -U episense -d episense_analysis

# Comandos úteis no psql:
# \dt analysis.*          - Listar tabelas no schema analysis
# \d analysis.nome_tabela - Descrever uma tabela
# \l                      - Listar bancos de dados
# \q                      - Sair
```

## 📊 Verificar o banco de dados

```sql
-- Listar todas as tabelas
SELECT table_schema, table_name 
FROM information_schema.tables 
WHERE table_schema = 'analysis';

-- Ver migrations aplicadas
SELECT * FROM analysis."__EFMigrationsHistory";
```

## 🛑 Parar e Remover o PostgreSQL

```powershell
# Parar o container
docker-compose stop postgres

# Parar e remover (os dados permanecem no volume)
docker-compose down postgres

# Remover TUDO incluindo dados
docker-compose down -v
```

## 🔍 Logs e Troubleshooting

```powershell
# Ver logs em tempo real
docker-compose logs -f postgres

# Ver últimas 100 linhas
docker-compose logs --tail=100 postgres

# Verificar saúde do container
docker inspect episense-postgres --format='{{.State.Health.Status}}'
```

## 🔐 Segurança (Produção)

Para produção, crie um arquivo `.env` na raiz:

```env
POSTGRES_PASSWORD=sua_senha_super_segura
POSTGRES_USER=episense_prod
```

E atualize o `docker-compose.yml`:

```yaml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  POSTGRES_USER: ${POSTGRES_USER}
```

## 💡 Dicas

1. **Backup**: Use `pg_dump` para fazer backup regular
   ```powershell
   docker exec episense-postgres pg_dump -U episense episense_analysis > backup.sql
   ```

2. **Restore**: Restaurar um backup
   ```powershell
   cat backup.sql | docker exec -i episense-postgres psql -U episense episense_analysis
   ```

3. **Performance**: Ajuste os parâmetros do PostgreSQL editando `shared_buffers`, `work_mem`, etc.

4. **Monitoramento**: Use ferramentas como pgAdmin, DBeaver ou Azure Data Studio para gerenciar o banco visualmente
