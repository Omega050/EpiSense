# 🚀 EpiSense - Roadmap de Implementação

## 📋 Visão Geral
Sistema de detecção de anomalias epidemiológicas com agregação de dados e alertas automatizados.

---

## 🔨 ITERAÇÃO 1: Sistema de Agregação (Base)
**Objetivo:** Cache de contagens diárias por município/flag para consultas rápidas.

### 1.1 AggregationService
**Arquivo:** `backend/src/Contexts/EpiSense.Analysis/Services/AggregationService.cs`

- [ ] Criar classe com injeção de `AnalysisDbContext` e `ILogger`
- [ ] Implementar `UpdateDailyAggregationsAsync(DateTime targetDate)`
  - Buscar `ObservationSummary` do dia (com `CodigoMunicipioIBGE != null`)
  - Expandir apenas flags clínicas (`SUSPEITA_DENGUE`, `ANEMIA_FERROPRIVA`)
  - Agrupar por (município, data, flag) e contar
  - UPSERT em `DailyCaseAggregation`
- [ ] Implementar `RebuildAllAggregationsAsync()` (mesma lógica, sem filtro de data)
- [ ] Criar método auxiliar `IsValidClinicalFlag(string flag)`

### 1.2 AggregationCacheJob
**Arquivo:** `backend/src/Apps/EpiSense.Api/Jobs/AggregationCacheJob.cs`

- [ ] Criar job Hangfire com `[AutomaticRetry(Attempts = 3)]`
- [ ] Implementar `UpdateDailyAggregations()` (processa dia anterior)
- [ ] Implementar `RebuildAllAggregations()` (job manual)
- [ ] Adicionar logs estruturados

### 1.3 Registro no Program.cs
- [ ] Registrar `AggregationService` e `AggregationCacheJob` como Scoped
- [ ] Configurar Recurring Job (Hangfire) para execução diária às 2h UTC
- [ ] Testar via `/hangfire` Dashboard

### 1.4 Endpoint de Consulta
**Arquivo:** `backend/src/Apps/EpiSense.Api/Controllers/AnalysisController.cs`

- [ ] `GET /api/analysis/aggregations` (filtros: município, flag, datas)
- [ ] `GET /api/analysis/aggregations/summary` (estatísticas gerais)

**DONE quando:** Job roda automaticamente e endpoint retorna dados válidos.

---

## 📊 ITERAÇÃO 2: Detecção de Anomalias - Shewhart
**Objetivo:** Controle estatístico para detectar surtos epidemiológicos.

### 2.1 ShewhartAnalyzer
**Arquivo:** `backend/src/Contexts/EpiSense.Analysis/Services/ShewhartAnalyzer.cs`

- [ ] Criar classe `ShewhartAnalyzer`
- [ ] Implementar `AnalyzeAsync(string municipioIbge, string flag, int windowDays = 30)`
  - Calcular média móvel e desvio padrão
  - Calcular limites de controle (LCL, UCL = média ± 3σ)
  - Aplicar regras de Western Electric (1 ponto > 3σ, 2/3 > 2σ, etc.)
  - Retornar `ShewhartResult` com lista de anomalias

### 2.2 AnalysisResult Entity
- [ ] Verificar/ajustar `Domain/Entities/AnalysisResult.cs`
- [ ] Garantir campos: `MunicipioIBGE`, `Flag`, `AnalysisType`, `AnomalyDetected`, `Severity`, `Details` (JSONB)

### 2.3 AnomalyDetectionJob
**Arquivo:** `backend/src/Apps/EpiSense.Api/Jobs/AnomalyDetectionJob.cs`

- [ ] Criar job `RunShewhartAnalysisAsync()`
- [ ] Iterar sobre todos os municípios e flags
- [ ] Executar análise e salvar resultados
- [ ] Registrar como Recurring Job (após agregação, às 3h)

### 2.4 Endpoint de Anomalias
- [ ] `GET /api/analysis/anomalies` (filtros: município, flag, severidade)
- [ ] `GET /api/analysis/anomalies/chart/{municipio}/{flag}` (dados para visualização)

**DONE quando:** Shewhart detecta anomalia artificial injetada e salva em `AnalysisResult`.

---

## 📈 ITERAÇÃO 3: CUSUM Algorithm
**Objetivo:** Detectar mudanças sutis e persistentes (shifts graduais).

### 3.1 CUSUMAnalyzer
**Arquivo:** `backend/src/Contexts/EpiSense.Analysis/Services/CUSUMAnalyzer.cs`

- [ ] Implementar algoritmo CUSUM (h=5, k=0.5)
- [ ] Detectar upward/downward shifts
- [ ] Retornar `CUSUMResult` com anomalias

### 3.2 Integração
- [ ] Adicionar `RunCUSUMAnalysisAsync()` ao `AnomalyDetectionJob`
- [ ] Salvar com `AnalysisType = "CUSUM"`

**DONE quando:** CUSUM detecta shift gradual que Shewhart não detecta.

---

## 🚨 ITERAÇÃO 4: Sistema de Alertas
**Objetivo:** Notificar autoridades quando anomalias forem detectadas.

### 4.1 AlertService
**Arquivo:** `backend/src/Contexts/EpiSense.Alerts/AlertService.cs`

- [ ] Implementar `SendAnomalyAlertAsync(AnalysisResult result)`
- [ ] Criar entidade `Alert` (tabela `alerts`)
- [ ] Classificar severidade: LOW, MEDIUM, HIGH, CRITICAL (baseado em σ)
- [ ] Canais: Log estruturado (imediato), Email/Webhook (futuro)

### 4.2 Integração
- [ ] Injetar `AlertService` no `AnomalyDetectionJob`
- [ ] Disparar alerta após detecção (com debounce)

### 4.3 AlertsController
- [ ] `GET /api/alerts` (listar, paginado)
- [ ] `GET /api/alerts/unacknowledged` (alertas pendentes)
- [ ] `PUT /api/alerts/{id}/acknowledge` (marcar como visto)

**DONE quando:** Alerta é criado automaticamente e endpoint funciona.

---

## 📊 ITERAÇÃO 5: Dashboard
**Objetivo:** Interface de monitoramento em tempo real.

### 5.1 DashboardController
**Arquivo:** `backend/src/Apps/EpiSense.Api/Controllers/DashboardController.cs`

- [ ] `GET /api/dashboard/overview` (métricas gerais)
- [ ] `GET /api/dashboard/trends` (séries temporais)
- [ ] `GET /api/dashboard/map` (dados geográficos - GeoJSON)

### 5.2 Frontend (Opcional)
- [ ] React/Next.js + Recharts + Leaflet
- [ ] Telas: Dashboard, Mapa de calor, Gráficos Shewhart, Lista de alertas

**DONE quando:** Dashboard carrega < 2s e exibe dados reais.

---

## 🔧 ITERAÇÃO 6: Produção
### 6.1 Performance
- [ ] Índices compostos no PostgreSQL
- [ ] Cache Redis para agregações frequentes
- [ ] Paginação em todos os endpoints

### 6.2 Observabilidade
- [ ] Serilog com sink estruturado
- [ ] Métricas (Prometheus)
- [ ] Health checks avançados
- [ ] Distributed tracing (OpenTelemetry)

### 6.3 Documentação
- [ ] Swagger/OpenAPI completo
- [ ] README com setup
- [ ] ADRs atualizados
- [ ] Runbook operacional

---

## 📅 Cronograma Estimado
| Iteração | Duração | Dependências |
|----------|---------|--------------|
| 1 - Agregação | 2-3 dias | Nenhuma |
| 2 - Shewhart | 3-4 dias | Iteração 1 |
| 3 - CUSUM | 2 dias | Iteração 2 |
| 4 - Alertas | 2 dias | Iteração 2/3 |
| 5 - Dashboard | 5-7 dias | Iterações 1-4 |
| 6 - Produção | Contínuo | Após deploy |

---

## ✅ Critérios de Aceitação Gerais
- **Iteração 1:** Job diário funciona + endpoint retorna dados
- **Iteração 2:** Shewhart detecta anomalia + salva resultado
- **Iteração 3:** CUSUM detecta shift gradual
- **Iteração 4:** Alertas criados automaticamente
- **Iteração 5:** Dashboard funcional com dados reais
