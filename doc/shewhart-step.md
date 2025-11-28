# 📊 Detecção de Anomalias - Shewhart
**Objetivo:** Controle estatístico para detectar surtos epidemiológicos.

### 2.1 ShewhartAnalyzer ✅
**Arquivo:** `backend/src/Contexts/EpiSense.Analysis/Services/ShewhartAnalyzer.cs`

- [x] Criar classe `ShewhartAnalyzer`
- [x] Implementar `AnalyzeAsync(string municipioIbge, string flag, DateTime? targetDate, int baselineDays)`
  - Calcular média e desvio padrão do baseline
  - Calcular limites de controle (LCL, UCL = média ± 3σ)
  - Detectar anomalias (AbruptIncrease, AbruptDecrease)
  - Classificar severidade (Low, Medium, High, Critical)
  - Retornar `ShewhartResult` com detalhes completos

### 2.2 ValueObjects ✅
**Arquivos:** `backend/src/Contexts/EpiSense.Analysis/Domain/ValueObjects/`

- [x] `ShewhartResult.cs` - Resultado completo da análise
- [x] `BaselineStatistics.cs` - Estatísticas do baseline (μ, σ, UCL, LCL)
- [x] `DailyCaseCount.cs` - Contagem de casos por dia
- [x] `AnomalyType.cs` - Enum (None, AbruptIncrease, AbruptDecrease)
- [x] `AnomalySeverity.cs` - Enum (None, Low, Medium, High, Critical)

### 2.3 ShewhartAnalysisJob ✅
**Arquivo:** `backend/src/Apps/EpiSense.Api/Jobs/ShewhartAnalysisJob.cs`

- [x] Criar job `ExecuteAsync()` com estratégia temporal D-2
- [x] Iterar sobre todos os municípios e flags configurados
- [x] Executar análise Shewhart para cada combinação
- [x] Registrar logs detalhados de anomalias detectadas
- [x] Registrar como Recurring Job (a cada 2 horas) no Hangfire

### 2.4 Endpoint de Anomalias ✅
**Arquivo:** `backend/src/Apps/EpiSense.Api/Controllers/AnomalyController.cs`

- [x] `GET /api/anomaly/analyze/{municipioIbge}/{flag}` - Análise manual
- [x] `POST /api/anomaly/trigger-analysis` - Força execução do job
- [x] `GET /api/anomaly/config` - Configurações do algoritmo

### 2.5 Ajustes na Agregação ✅
**Arquivo:** `backend/src/Apps/EpiSense.Api/Jobs/AggregationJob.cs`

- [x] Modificar para agregar D-2 (não D-1)
- [x] Garantir separação entre dados frescos (D-0, D-1) e consolidados (D-2+)

### 2.6 Documentação ✅
**Arquivos:** `doc/`

- [x] `shewhart-temporal-strategy.md` - Estratégia temporal completa
- [x] `diagrams/shewhart-architecture.puml` - Diagrama de arquitetura

## ✅ DONE

**Implementação completa:**
- ✅ Shewhart detecta anomalias usando μ ± 3σ
- ✅ Job recorrente executa a cada 2 horas
- ✅ Estratégia temporal D-2 para dados consolidados
- ✅ API REST para análises manuais
- ✅ Logs detalhados e dashboard Hangfire
- ✅ Documentação técnica completa

**Próximos passos sugeridos:**
- [ ] Persistir resultados em tabela `shewhart_results`
- [ ] Criar dashboard de visualização de anomalias
- [ ] Implementar notificações/alertas automáticos
- [ ] Adicionar regras Western Electric complementares
- [ ] Implementar cache de baseline para performance