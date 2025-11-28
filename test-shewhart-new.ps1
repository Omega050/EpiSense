# ========================================
# Script de Teste - Deteccao de Anomalias Shewhart
# ========================================
# Testa a detecao de anomalias epidemiologicas usando algoritmo Shewhart.
# 
# IMPORTANTE: Todas as flags relacionadas a SIB (SIB_SUSPEITA, SIB_GRAVE, 
# DESVIO_ESQUERDA, etc.) sao normalizadas para SIB_SUSPEITA nas agregacoes.
# Portanto, voce pode usar qualquer uma dessas flags no parametro -Flag.
# ========================================

param(
    [string]$ApiUrl = "http://localhost:5080",
    [string]$Flag = "SIB_SUSPEITA"  # Ou DESVIO_ESQUERDA, SIB_GRAVE, etc. (todos mapeiam para SIB_SUSPEITA)
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TESTE - DETECCAO DE ANOMALIAS SHEWHART" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuracoes
$MunicipioIbge = "5221403"  # Trindade/GO
$today = Get-Date
$targetDate = $today.AddDays(-2)
$baselineStart = $targetDate.AddDays(-15)

Write-Host "Configuracao:" -ForegroundColor Yellow
Write-Host "   API: $ApiUrl"
Write-Host "   Municipio: $MunicipioIbge (Trindade/GO)"
Write-Host "   Flag: $Flag"
Write-Host "   Hoje: $($today.ToString('yyyy-MM-dd'))"
Write-Host "   Target (D-2): $($targetDate.ToString('yyyy-MM-dd'))"
Write-Host "   Baseline: $($baselineStart.ToString('yyyy-MM-dd')) ate $($targetDate.AddDays(-1).ToString('yyyy-MM-dd'))"
Write-Host ""

# ========================================
# ETAPA 1: Inserir Dados de Baseline
# ========================================
Write-Host "ETAPA 1: Inserindo dados de baseline..." -ForegroundColor Cyan

$totalBaseline = 0
for ($dia = 0; $dia -lt 15; $dia++) {
    $dataColeta = $baselineStart.AddDays($dia)
    $casosNoDia = Get-Random -Minimum 9 -Maximum 11
    
    for ($caso = 0; $caso -lt $casosNoDia; $caso++) {
        $bundle = @{
            resourceType = "Bundle"
            id = "bundle-baseline-$dia-$caso"
            type = "collection"
            entry = @(
                @{
                    fullUrl = "urn:uuid:patient-bl-$dia-$caso"
                    resource = @{
                        resourceType = "Patient"
                        id = "patient-bl-$dia-$caso"
                        name = @(@{
                            use = "official"
                            family = "Silva"
                            given = @("Paciente")
                        })
                        address = @(@{
                            use = "home"
                            city = "Trindade"
                            state = "GO"
                            postalCode = "75380-000"
                            country = "BRA"
                        })
                    }
                },
                @{
                    fullUrl = "urn:uuid:enc-bl-$dia-$caso"
                    resource = @{
                        resourceType = "Encounter"
                        id = "enc-bl-$dia-$caso"
                        status = "finished"
                        class = @{
                            system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"
                            code = "AMB"
                            display = "ambulatory"
                        }
                        subject = @{
                            reference = "urn:uuid:patient-bl-$dia-$caso"
                        }
                        period = @{
                            start = $dataColeta.ToString("yyyy-MM-ddT10:00:00-03:00")
                            end = $dataColeta.ToString("yyyy-MM-ddT10:30:00-03:00")
                        }
                    }
                },
                @{
                    fullUrl = "urn:uuid:obs-bl-$dia-$caso"
                    resource = @{
                        resourceType = "Observation"
                        id = "obs-bl-$dia-$caso"
                        status = "final"
                        category = @(@{
                            coding = @(@{
                                system = "http://terminology.hl7.org/CodeSystem/observation-category"
                                code = "laboratory"
                                display = "Laboratory"
                            })
                        })
                        code = @{
                            coding = @(@{
                                system = "http://loinc.org"
                                code = "58410-2"
                                display = "Complete blood count with auto differential panel"
                            })
                        }
                        subject = @{
                            reference = "urn:uuid:patient-bl-$dia-$caso"
                        }
                        encounter = @{
                            reference = "urn:uuid:enc-bl-$dia-$caso"
                        }
                        effectiveDateTime = $dataColeta.ToString("yyyy-MM-ddT10:15:00-03:00")
                        component = @(
                            @{
                                code = @{
                                    coding = @(@{
                                        system = "http://loinc.org"
                                        code = "6690-2"
                                        display = "Leukocytes [#/volume] in Blood"
                                    })
                                }
                                valueQuantity = @{
                                    value = [math]::Round((Get-Random -Minimum 11.2 -Maximum 11.8), 1)
                                    unit = "x10*3/uL"
                                    system = "http://unitsofmeasure.org"
                                    code = "10*3/uL"
                                }
                                referenceRange = @(@{
                                    low = @{ value = 4.0 }
                                    high = @{ value = 11.0 }
                                })
                            },
                            @{
                                code = @{
                                    coding = @(@{
                                        system = "http://loinc.org"
                                        code = "751-8"
                                        display = "Neutrophils [#/volume] in Blood"
                                    })
                                }
                                valueQuantity = @{
                                    value = [math]::Round((Get-Random -Minimum 7.6 -Maximum 8.2), 1)
                                    unit = "x10*3/uL"
                                    system = "http://unitsofmeasure.org"
                                    code = "10*3/uL"
                                }
                                referenceRange = @(@{
                                    low = @{ value = 2.0 }
                                    high = @{ value = 7.5 }
                                })
                            },
                            @{
                                code = @{
                                    coding = @(@{
                                        system = "http://loinc.org"
                                        code = "764-1"
                                        display = "Stabs [#/volume] in Blood"
                                    })
                                }
                                valueQuantity = @{
                                    value = [math]::Round((Get-Random -Minimum 0.1 -Maximum 0.4), 1)
                                    unit = "x10*3/uL"
                                    system = "http://unitsofmeasure.org"
                                    code = "10*3/uL"
                                }
                                referenceRange = @(@{
                                    low = @{ value = 0.0 }
                                    high = @{ value = 0.5 }
                                })
                            }
                        )
                    }
                }
            )
        }
        
        try {
            $json = $bundle | ConvertTo-Json -Depth 20 -Compress
            $null = Invoke-RestMethod -Uri "$ApiUrl/api/ingestion/observation" -Method Post -Body $json -ContentType "application/json" -ErrorAction Stop
            $totalBaseline++
        }
        catch {
            Write-Host "   Erro: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host "   Dia $($dataColeta.ToString('yyyy-MM-dd')): $casosNoDia casos" -ForegroundColor Gray
}

Write-Host "   Total baseline: $totalBaseline casos" -ForegroundColor Green
Write-Host ""

# ========================================
# ETAPA 2: Inserir Anomalia (D-2)
# ========================================
Write-Host "ETAPA 2: Inserindo anomalia em D-2..." -ForegroundColor Cyan
Write-Host "   Simulando SURTO EPIDEMICO com casos graves..." -ForegroundColor Yellow

$totalAnomalia = 0
for ($caso = 0; $caso -lt 100; $caso++) {
    $bundle = @{
        resourceType = "Bundle"
        id = "bundle-anomalia-$caso"
        type = "collection"
        entry = @(
            @{
                fullUrl = "urn:uuid:patient-ano-$caso"
                resource = @{
                    resourceType = "Patient"
                    id = "patient-ano-$caso"
                    name = @(@{
                        use = "official"
                        family = "Pereira"
                        given = @("Ana")
                    })
                    address = @(@{
                        use = "home"
                        city = "Trindade"
                        state = "GO"
                        postalCode = "75380-000"
                        country = "BRA"
                    })
                }
            },
            @{
                fullUrl = "urn:uuid:enc-ano-$caso"
                resource = @{
                    resourceType = "Encounter"
                    id = "enc-ano-$caso"
                    status = "finished"
                    class = @{
                        system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"
                        code = "AMB"
                        display = "ambulatory"
                    }
                    subject = @{
                        reference = "urn:uuid:patient-ano-$caso"
                    }
                    period = @{
                        start = $targetDate.ToString("yyyy-MM-ddT10:00:00-03:00")
                        end = $targetDate.ToString("yyyy-MM-ddT10:30:00-03:00")
                    }
                }
            },
            @{
                fullUrl = "urn:uuid:obs-ano-$caso"
                resource = @{
                    resourceType = "Observation"
                    id = "obs-ano-$caso"
                    status = "final"
                    category = @(@{
                        coding = @(@{
                            system = "http://terminology.hl7.org/CodeSystem/observation-category"
                            code = "laboratory"
                            display = "Laboratory"
                        })
                    })
                    code = @{
                        coding = @(@{
                            system = "http://loinc.org"
                            code = "58410-2"
                            display = "Complete blood count with auto differential panel"
                        })
                    }
                    subject = @{
                        reference = "urn:uuid:patient-ano-$caso"
                    }
                    encounter = @{
                        reference = "urn:uuid:enc-ano-$caso"
                    }
                    effectiveDateTime = $targetDate.ToString("yyyy-MM-ddT10:15:00-03:00")
                    component = @(
                        @{
                            code = @{
                                coding = @(@{
                                    system = "http://loinc.org"
                                    code = "6690-2"
                                    display = "Leukocytes [#/volume] in Blood"
                                })
                            }
                            valueQuantity = @{
                                value = [math]::Round((Get-Random -Minimum 15.0 -Maximum 20.0), 1)
                                unit = "x10*3/uL"
                                system = "http://unitsofmeasure.org"
                                code = "10*3/uL"
                            }
                            referenceRange = @(@{
                                low = @{ value = 4.0 }
                                high = @{ value = 11.0 }
                            })
                        },
                        @{
                            code = @{
                                coding = @(@{
                                    system = "http://loinc.org"
                                    code = "751-8"
                                    display = "Neutrophils [#/volume] in Blood"
                                })
                            }
                            valueQuantity = @{
                                value = [math]::Round((Get-Random -Minimum 10.0 -Maximum 14.0), 1)
                                unit = "x10*3/uL"
                                system = "http://unitsofmeasure.org"
                                code = "10*3/uL"
                            }
                            referenceRange = @(@{
                                low = @{ value = 2.0 }
                                high = @{ value = 7.5 }
                            })
                        },
                        @{
                            code = @{
                                coding = @(@{
                                    system = "http://loinc.org"
                                    code = "764-1"
                                    display = "Stabs [#/volume] in Blood"
                                })
                            }
                            valueQuantity = @{
                                value = [math]::Round((Get-Random -Minimum 1.2 -Maximum 2.5), 1)
                                unit = "x10*3/uL"
                                system = "http://unitsofmeasure.org"
                                code = "10*3/uL"
                            }
                            referenceRange = @(@{
                                low = @{ value = 0.0 }
                                high = @{ value = 0.5 }
                            })
                        }
                    )
                }
            }
        )
    }
    
    try {
        $json = $bundle | ConvertTo-Json -Depth 20 -Compress
        $null = Invoke-RestMethod -Uri "$ApiUrl/api/ingestion/observation" -Method Post -Body $json -ContentType "application/json" -ErrorAction Stop
        $totalAnomalia++
    }
    catch {
        Write-Host "   Erro: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "   Total anomalia: $totalAnomalia casos" -ForegroundColor Green
Write-Host ""

# ========================================
# ETAPA 3: Executar Agregacao Diaria em Lote
# ========================================
Write-Host "ETAPA 3: Executando agregacao diaria em lote..." -ForegroundColor Cyan

try {
    $aggregationUrl = "$ApiUrl/api/analysis/aggregations/rebuild"
    Write-Host "   POST $aggregationUrl" -ForegroundColor Gray
    
    $aggResult = Invoke-RestMethod -Uri $aggregationUrl -Method Post -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Agregacao em lote concluida: $($aggResult.message)" -ForegroundColor Green
}
catch {
    Write-Host "   Erro na agregacao: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Continuando mesmo assim..." -ForegroundColor Yellow
}

Write-Host ""

# ========================================
# ETAPA 4: Aguardar Processamento
# ========================================
Write-Host "ETAPA 4: Aguardando processamento..." -ForegroundColor Cyan
Start-Sleep -Seconds 3
Write-Host "   Pronto" -ForegroundColor Green
Write-Host ""

# ========================================
# ETAPA 5: Executar Analise Shewhart
# ========================================
Write-Host "ETAPA 5: Executando analise Shewhart..." -ForegroundColor Cyan

try {
    $analysisUrl = "$ApiUrl/api/anomaly/analyze/$MunicipioIbge/$Flag"
    Write-Host "   GET $analysisUrl" -ForegroundColor Gray
    
    $result = Invoke-RestMethod -Uri $analysisUrl -Method Get -ErrorAction Stop
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  RESULTADO DA ANALISE" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($result.anomalyDetected) {
        Write-Host "SUCESSO! Anomalia detectada!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Municipio: $($result.municipioIbge)" -ForegroundColor White
        Write-Host "Flag: $($result.flag)" -ForegroundColor White
        Write-Host "Data: $($result.targetDate)" -ForegroundColor White
        Write-Host ""
        Write-Host "Baseline:" -ForegroundColor Yellow
        Write-Host "   Media: $($result.baseline.mean)" -ForegroundColor White
        Write-Host "   Desvio Padrao: $($result.baseline.stdDev)" -ForegroundColor White
        Write-Host "   LCL: $($result.baseline.lcl)" -ForegroundColor White
        Write-Host "   UCL: $($result.baseline.ucl)" -ForegroundColor White
        Write-Host ""
        Write-Host "Observado:" -ForegroundColor Yellow
        Write-Host "   Valor: $($result.observedValue)" -ForegroundColor White
        Write-Host "   Z-Score: $($result.zScore)" -ForegroundColor $(if ($result.zScore -gt 3) { "Red" } else { "White" })
        Write-Host ""
        Write-Host "Anomalia:" -ForegroundColor Yellow
        Write-Host "   Tipo: $($result.anomalyType)" -ForegroundColor White
        Write-Host "   Severidade: $($result.severity)" -ForegroundColor White
        Write-Host ""
        
        exit 0
    }
    elseif ($result.insufficientData) {
        Write-Host "DADOS INSUFICIENTES" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Nao ha dados suficientes para analise." -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
    else {
        Write-Host "TESTE FALHOU: Anomalia NAO detectada" -ForegroundColor Red
        Write-Host ""
        Write-Host "Esperado: Deteccao de anomalia ($totalAnomalia casos vs ~10 de baseline)" -ForegroundColor Yellow
        Write-Host "Obtido: Nenhuma anomalia detectada" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Baseline:" -ForegroundColor Yellow
        Write-Host "   Media: $($result.baseline.mean)" -ForegroundColor White
        Write-Host "   Desvio Padrao: $($result.baseline.stdDev)" -ForegroundColor White
        Write-Host ""
        Write-Host "Observado:" -ForegroundColor Yellow
        Write-Host "   Valor: $($result.observedValue)" -ForegroundColor White
        Write-Host ""
        
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "ERRO ao executar analise:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Verifique:" -ForegroundColor Yellow
    Write-Host "   1. API rodando em $ApiUrl" -ForegroundColor Yellow
    Write-Host "   2. Endpoint GET /api/anomaly/analyze/{municipioIbge}/{flag} acessivel" -ForegroundColor Yellow
    Write-Host "   3. Dados inseridos corretamente no PostgreSQL" -ForegroundColor Yellow
    Write-Host "   4. Municipio $MunicipioIbge existe nos dados" -ForegroundColor Yellow
    Write-Host ""
    
    exit 1
}
