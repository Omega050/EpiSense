# 📊 Detecção de Anomalias - Shewhart
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