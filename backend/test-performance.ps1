#!/usr/bin/env pwsh
# Script de Teste de Performance: Hangfire vs Callback
# Simula ingestao de 5 hemogramas sequenciais

Write-Host "TESTE DE PERFORMANCE: HANGFIRE VS CALLBACK" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Configuracao da URL (ajuste conforme seu launchSettings.json)
$baseUrl = "http://localhost:5080"
Write-Host "URL da API: $baseUrl" -ForegroundColor Gray
Write-Host ""

# Verificar se API esta rodando
try {
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "API esta respondendo!" -ForegroundColor Green
} catch {
    Write-Host "ERRO: API nao esta rodando em $baseUrl" -ForegroundColor Red
    Write-Host "Execute antes: cd backend\src\Apps\EpiSense.Api; dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

$hemogramas = @()

# Gera 1000 hemogramas FHIR de exemplo
for ($i = 1; $i -le 1000; $i++) {
    $hemogramas += @{
        resourceType = "Observation"
        id = "hemogram-$i"
        status = "final"
        category = @(
            @{
                coding = @(
                    @{
                        system = "http://terminology.hl7.org/CodeSystem/observation-category"
                        code = "laboratory"
                    }
                )
            }
        )
        code = @{
            coding = @(
                @{
                    system = "http://loinc.org"
                    code = "58410-2"
                    display = "Complete blood count (hemogram)"
                }
            )
        }
        subject = @{
            reference = "Patient/patient-$i"
        }
        effectiveDateTime = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
        component = @(
            @{
                code = @{
                    coding = @(
                        @{
                            system = "http://loinc.org"
                            code = "26464-8"
                            display = "Leukocytes [#/volume] in Blood"
                        }
                    )
                }
                valueQuantity = @{
                    value = 15000 + ($i * 1000)
                    unit = "cells/uL"
                    system = "http://unitsofmeasure.org"
                    code = "/uL"
                }
            }
        )
    }
}

# Funcao para enviar hemograma
function Send-Hemogram {
    param(
        [Parameter(Mandatory=$true)]
        [hashtable]$Hemogram,
        
        [Parameter(Mandatory=$true)]
        [int]$Index,
        
        [Parameter(Mandatory=$false)]
        [string]$Endpoint = "/api/ingestion/observation"
    )
    
    $body = $Hemogram | ConvertTo-Json -Depth 10
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $response = Invoke-RestMethod `
            -Uri "$baseUrl$Endpoint" `
            -Method Post `
            -Body $body `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        $stopwatch.Stop()
        
        return @{
            Success = $true
            Time = $stopwatch.ElapsedMilliseconds
            DataId = $response.DataId
            JobId = $response.JobId
            Mode = $response.Mode
        }
    }
    catch {
        $stopwatch.Stop()
        return @{
            Success = $false
            Time = $stopwatch.ElapsedMilliseconds
            Error = $_.Exception.Message
        }
    }
}

# TESTE 1: HANGFIRE (Implementacao Atual)
Write-Host "TESTE 1: HANGFIRE (Implementacao Atual)" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Yellow

$hangfireResults = @()
$hangfireTotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "  Enviando 1000 requisicoes..." -ForegroundColor Gray
for ($i = 0; $i -lt 1000; $i++) {
    if (($i + 1) % 50 -eq 0) {
        Write-Host "    Progresso: $($i + 1)/1000..." -ForegroundColor DarkGray
    }
    Write-Host "  Enviando hemograma $($i + 1)..." -NoNewline
    
    $result = Send-Hemogram -Hemogram $hemogramas[$i] -Index ($i + 1) -Endpoint "/api/ingestion/observation"
    $hangfireResults += $result
    
    if ($result.Success) {
        Write-Host " OK - $($result.Time)ms [Mode: $($result.Mode)]" -ForegroundColor Green
    }
    else {
        Write-Host " ERRO - $($result.Time)ms - $($result.Error)" -ForegroundColor Red
    }
}

$hangfireTotalStopwatch.Stop()

Write-Host ""
Write-Host "Resultados Hangfire:" -ForegroundColor Cyan
Write-Host "  Tempo total (latencia do cliente): $($hangfireTotalStopwatch.ElapsedMilliseconds)ms"
$hangfireSuccessful = @($hangfireResults | Where-Object {$_.Success})
$hangfireAvgTime = 0
if ($hangfireSuccessful.Count -gt 0) {
    $totalTime = 0
    foreach ($result in $hangfireSuccessful) {
        $totalTime += $result.Time
    }
    $hangfireAvgTime = [math]::Round($totalTime / $hangfireSuccessful.Count, 2)
    Write-Host "  Tempo medio por requisicao: ${hangfireAvgTime}ms"
}
Write-Host "  Requisicoes bem-sucedidas: $($hangfireSuccessful.Count)/1000"

# Aguardar alguns segundos para processamento assincrono
Write-Host ""
Write-Host "Aguardando processamento assincrono (5 segundos)..." -ForegroundColor Gray
Start-Sleep -Seconds 5

Write-Host ""
Write-Host ""

# TESTE 2: CALLBACK (Implementacao Real)
Write-Host "TESTE 2: CALLBACK (Implementacao Real)" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Yellow

$callbackResults = @()
$callbackTotalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "  Enviando 1000 requisicoes..." -ForegroundColor Gray
for ($i = 0; $i -lt 1000; $i++) {
    if (($i + 1) % 50 -eq 0) {
        Write-Host "    Progresso: $($i + 1)/1000..." -ForegroundColor DarkGray
    }
    
    $result = Send-Hemogram -Hemogram $hemogramas[$i] -Index ($i + 1) -Endpoint "/api/ingestion/observation/callback"
    $callbackResults += $result
    
    if (!$result.Success) {
        Write-Host "  ERRO na requisicao $($i + 1): $($result.Error)" -ForegroundColor Red
    }
}

Write-Host "  Todas as requisicoes Callback enviadas!" -ForegroundColor Green

$callbackTotalStopwatch.Stop()

# Calcular totais
$callbackTotalTime = 0
foreach ($result in $callbackResults) {
    $callbackTotalTime += $result.Time
}

Write-Host ""
Write-Host "Resultados Callback (Real):" -ForegroundColor Cyan
Write-Host "  Tempo total: ${callbackTotalTime}ms"

$callbackAvgTime = 0
if ($callbackResults.Count -gt 0) {
    $callbackAvgTime = [math]::Round($callbackTotalTime / $callbackResults.Count, 2)
}
Write-Host "  Tempo medio por requisicao: ${callbackAvgTime}ms"
Write-Host "  Requisicoes bem-sucedidas: $(@($callbackResults | Where-Object {$_.Success}).Count)/1000"

# COMPARACAO
Write-Host ""
Write-Host ""
Write-Host "COMPARACAO FINAL" -ForegroundColor Magenta
Write-Host "==================" -ForegroundColor Magenta

$hangfireAvg = $hangfireAvgTime
$callbackAvg = $callbackAvgTime

if ($hangfireAvg -gt 0 -and $callbackAvg -gt 0) {
    $speedup = [math]::Round($callbackAvg / $hangfireAvg, 2)
} else {
    $speedup = 0
}

$diffTotal = $callbackTotalTime - $hangfireTotalStopwatch.ElapsedMilliseconds
$diffAvg = [math]::Round($callbackAvg - $hangfireAvg, 2)

Write-Host ""
Write-Host "Metrica                          | Hangfire      | Callback      | Diferenca" -ForegroundColor White
Write-Host "-------------------------------- + ------------- + ------------- + -------------" -ForegroundColor White
Write-Host "Tempo Total (1000 hemogramas)    | $($hangfireTotalStopwatch.ElapsedMilliseconds)ms | ${callbackTotalTime}ms | ${diffTotal}ms (${speedup}x)" -ForegroundColor White
Write-Host "Tempo Medio por Requisicao       | ${hangfireAvg}ms | ${callbackAvg}ms | ${diffAvg}ms" -ForegroundColor White

if ($hangfireAvg -lt $callbackAvg) {
    Write-Host "Experiencia do Usuario           | Rapida | Notavel | Hangfire melhor" -ForegroundColor White
} else {
    Write-Host "Experiencia do Usuario           | Notavel | Rapida | Callback melhor" -ForegroundColor White
}

Write-Host ""
Write-Host "Interpretacao:" -ForegroundColor Cyan
if ($hangfireAvg -lt $callbackAvg) {
    Write-Host "  * Hangfire e ~${speedup}x mais rapido na perspectiva do cliente"
    Write-Host "  * Callback garante processamento completo antes de responder"
    Write-Host "  * Para UX: Hangfire e superior (latencia < 100ms)"
    Write-Host "  * Para consistencia: Callback e superior (transacional)"
} else {
    Write-Host "  * Callback e ~${speedup}x mais rapido na perspectiva do cliente"
    Write-Host "  * Hangfire processa de forma assincrona"
    Write-Host "  * Ambas abordagens tem vantagens dependendo do caso de uso"
}

Write-Host ""
Write-Host "Teste concluido!" -ForegroundColor Green
Write-Host ""
Write-Host "Dica: Acesse http://localhost:5080/hangfire para ver o dashboard" -ForegroundColor Gray
