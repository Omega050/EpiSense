# Configuração do PostgreSQL para o Módulo de Análise

Este documento descreve a configuração do PostgreSQL para persistência do módulo de análise do EpiSense.

## Configuração do Docker

O PostgreSQL foi adicionado ao `docker-compose.yml` com as seguintes características:

- **Imagem**: postgres:16-alpine
- **Porta**: 5432
- **Banco de dados**: episense_analysis
- **Usuário**: episense
- **Senha (dev)**: episense_dev_pass

## Iniciar o PostgreSQL

Para iniciar o PostgreSQL junto com os outros serviços:

```powershell
docker-compose up -d postgres
```

Para verificar o status:

```powershell
docker-compose ps
```

Para ver os logs:

```powershell
docker-compose logs postgres
```

## Connection String

A connection string está configurada no `appsettings.json`:

```json
"ConnectionStrings": {
  "AnalysisDatabase": "Host=localhost;Port=5432;Database=episense_analysis;Username=episense;Password=episense_dev_pass"
}
```

## Entity Framework Core

O projeto `EpiSense.Analysis` agora inclui:

- **Npgsql.EntityFrameworkCore.PostgreSQL**: Provider do EF Core para PostgreSQL
- **Microsoft.EntityFrameworkCore.Design**: Ferramentas para migrations

### Criar uma Migration

```powershell
cd backend\src\Contexts\EpiSense.Analysis
dotnet ef migrations add InitialCreate
```

### Aplicar Migrations

```powershell
dotnet ef database update
```

## DbContext

O `AnalysisDbContext` está localizado em:
- `Infrastructure/AnalysisDbContext.cs`

Para adicionar entidades, edite o DbContext:

```csharp
public DbSet<SuaEntidade> SuasEntidades { get; set; }
```

## Script de Inicialização

O arquivo `docker-init/init-postgres.sql` é executado automaticamente na primeira vez que o container é criado e inclui:

- Criação de extensões úteis (uuid-ossp, pg_trgm)
- Schema "analysis" (opcional)
- Permissões apropriadas

## Conectar ao PostgreSQL

Você pode conectar ao PostgreSQL usando qualquer cliente (pgAdmin, DBeaver, etc.):

- **Host**: localhost
- **Port**: 5432
- **Database**: episense_analysis
- **Username**: episense
- **Password**: episense_dev_pass

Ou via linha de comando:

```powershell
docker exec -it episense-postgres psql -U episense -d episense_analysis
```

## Variáveis de Ambiente (Produção)

Para produção, altere a senha e considere usar variáveis de ambiente:

```yaml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

E defina no arquivo `.env`:

```
POSTGRES_PASSWORD=senha_segura_aqui
```
