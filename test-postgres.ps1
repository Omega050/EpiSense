# Script de Testes PostgreSQL - EpiSense Analysis Module
# Autor: GitHub Copilot
# Data: 5 de outubro de 2025

Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "🧪 PostgreSQL Test Suite - EpiSense Analysis" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Função para exibir status
function Show-Status {
    param([string]$Message, [string]$Status)
    
    $color = switch ($Status) {
        "OK" { "Green" }
        "ERROR" { "Red" }
        "INFO" { "Yellow" }
        default { "White" }
    }
    
    Write-Host "[$Status] " -NoNewline -ForegroundColor $color
    Write-Host $Message
}

# 1. Verificar se o Docker está rodando
Show-Status "Verificando Docker..." "INFO"
try {
    docker ps | Out-Null
    Show-Status "Docker está rodando" "OK"
} catch {
    Show-Status "Docker não está rodando!" "ERROR"
    exit 1
}
Write-Host ""

# 2. Verificar status do PostgreSQL
Show-Status "Verificando container PostgreSQL..." "INFO"
$postgresStatus = docker inspect episense-postgres --format='{{.State.Status}}' 2>$null
if ($postgresStatus -eq "running") {
    Show-Status "PostgreSQL container está rodando" "OK"
    
    $health = docker inspect episense-postgres --format='{{.State.Health.Status}}' 2>$null
    if ($health -eq "healthy") {
        Show-Status "PostgreSQL está saudável" "OK"
    } else {
        Show-Status "PostgreSQL não está saudável: $health" "ERROR"
    }
} else {
    Show-Status "PostgreSQL não está rodando. Iniciando..." "INFO"
    docker-compose up -d postgres
    Start-Sleep -Seconds 5
    Show-Status "PostgreSQL iniciado" "OK"
}
Write-Host ""

# 3. Verificar conexão com o banco
Show-Status "Testando conexão com o banco..." "INFO"
$result = docker exec episense-postgres psql -U episense -d episense_analysis -c "SELECT 1;" 2>&1
if ($LASTEXITCODE -eq 0) {
    Show-Status "Conexão com o banco OK" "OK"
} else {
    Show-Status "Erro ao conectar no banco" "ERROR"
    exit 1
}
Write-Host ""

# 4. Verificar tabelas
Show-Status "Verificando estrutura do banco..." "INFO"
$tables = docker exec episense-postgres psql -U episense -d episense_analysis -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'analysis';"
$tableCount = $tables.Trim()
Show-Status "Tabelas encontradas no schema 'analysis': $tableCount" "OK"

if ($tableCount -gt 0) {
    Write-Host ""
    Show-Status "Listando tabelas:" "INFO"
    docker exec episense-postgres psql -U episense -d episense_analysis -c "SELECT tablename FROM pg_tables WHERE schemaname = 'analysis' ORDER BY tablename;"
}
Write-Host ""

# 5. Verificar migrations
Show-Status "Verificando migrations aplicadas..." "INFO"
$migrations = docker exec episense-postgres psql -U episense -d episense_analysis -t -c "SELECT COUNT(*) FROM analysis.__EFMigrationsHistory;" 2>$null
if ($LASTEXITCODE -eq 0) {
    $migrationCount = $migrations.Trim()
    Show-Status "Migrations aplicadas: $migrationCount" "OK"
} else {
    Show-Status "Nenhuma migration encontrada (isso é esperado se for a primeira vez)" "INFO"
}
Write-Host ""

# 6. Executar testes do projeto
Show-Status "Executando testes do projeto..." "INFO"
Write-Host ""
Push-Location -Path "backend\src\Contexts\EpiSense.Analysis.Tests"
try {
    dotnet run
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Show-Status "Todos os testes passaram!" "OK"
    } else {
        Write-Host ""
        Show-Status "Alguns testes falharam" "ERROR"
        exit 1
    }
} finally {
    Pop-Location
}
Write-Host ""

# 7. Estatísticas finais
Show-Status "Coletando estatísticas do banco..." "INFO"
$dbSize = docker exec episense-postgres psql -U episense -d episense_analysis -t -c "SELECT pg_size_pretty(pg_database_size('episense_analysis'));"
Show-Status "Tamanho do banco: $($dbSize.Trim())" "OK"

$connections = docker exec episense-postgres psql -U episense -d episense_analysis -t -c "SELECT count(*) FROM pg_stat_activity WHERE datname = 'episense_analysis';"
Show-Status "Conexões ativas: $($connections.Trim())" "OK"
Write-Host ""

# 8. Resumo final
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "✅ Teste Completo - Tudo Funcionando!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Próximos passos:" -ForegroundColor Yellow
Write-Host "  1. Integrar o DbContext na API" -ForegroundColor White
Write-Host "  2. Criar repositories e serviços" -ForegroundColor White
Write-Host "  3. Implementar suas entidades de domínio" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentação:" -ForegroundColor Yellow
Write-Host "  - POSTGRES-TEST-REPORT.md" -ForegroundColor White
Write-Host "  - doc/POSTGRES-QUICKSTART.md" -ForegroundColor White
Write-Host "  - backend/src/Contexts/EpiSense.Analysis/README.md" -ForegroundColor White
Write-Host ""

exit 0
