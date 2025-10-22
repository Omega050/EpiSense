# Guia Conceitual: Shewhart para Detecção de Anomalias Epidemiológicas

**Projeto:** EpiSense  
**Data:** 2025-10-20  
**Status:** Documento Conceitual

---

## 1. Visão Geral

A **Carta de Controle de Shewhart (Shewhart Control Chart)** é um método estatístico clássico de controle de qualidade que detecta desvios abruptos em um processo através da comparação de valores observados com limites estatísticos de controle. No contexto epidemiológico, é utilizado para identificar picos ou quedas súbitas no número de casos de doenças, sinalizando possíveis surtos ou eventos anormais.

### Objetivo no EpiSense
Monitorar a contagem diária de casos com flags clínicas específicas (ex: `SIB_SUSPEITA`, `SIB_GRAVE`) por município, detectando desvios abruptos que ultrapassem limites estatísticos de controle (μ ± 3σ).

---

## 2. Princípio de Funcionamento

### 2.1. Conceito Básico
Shewhart analisa cada ponto de dados **individualmente** e o compara com limites de controle baseados na distribuição estatística histórica. Se um valor cai fora desses limites (Upper Control Limit - UCL ou Lower Control Limit - LCL), uma anomalia é sinalizada.

**Analogia:** Como um termostato:
- Temperatura entre 18°C e 24°C → normal
- Temperatura > 24°C ou < 18°C → alarme imediato

### 2.2. Fórmula Simplificada

Para cada dia *i*, verificamos:

```
UCL = μ + 3σ  (Upper Control Limit)
LCL = μ - 3σ  (Lower Control Limit)

Se Xᵢ > UCL → Anomalia (aumento abrupto)
Se Xᵢ < LCL → Anomalia (diminuição abrupta)
```

Onde:
- **Xᵢ** = número de casos observados no dia *i*
- **μ** = média histórica de casos por dia
- **σ** = desvio padrão histórico

**Regra dos 3 Sigma:**
Em distribuição normal, 99.7% dos valores caem dentro de μ ± 3σ. Valores fora desse intervalo são **altamente improváveis** e indicam anomalia.

---

## 3. Parâmetros do Algoritmo

### 3.1. Média Histórica (μ)
Média de casos por dia em um período de referência.

**Exemplo:**
- Município X teve em média 15 casos de SIB/dia nos últimos 2 meses
- μ = 15

### 3.2. Desvio Padrão (σ)
Medida de variabilidade dos casos ao redor da média.

**Exemplo:**
- Casos variam tipicamente entre 10-20/dia
- σ ≈ 3.3

### 3.3. Limites de Controle

**Upper Control Limit (UCL):**
```
UCL = μ + 3σ
Exemplo: UCL = 15 + 3(3.3) = 24.9 ≈ 25 casos/dia
```

**Lower Control Limit (LCL):**
```
LCL = μ - 3σ (ou 0 se negativo)
Exemplo: LCL = 15 - 3(3.3) = 5.1 ≈ 5 casos/dia
```

**Interpretação:**
- Valores entre 5-25 casos/dia → normais
- Valores > 25 casos/dia → anomalia (possível surto)
- Valores < 5 casos/dia → anomalia (possível sub-notificação)

---

## 4. Fluxo de Análise

```
1. Cálculo de Baseline (Mensal)
   ↓
   Para cada município e flag clínica:
   - Buscar casos dos últimos 60 dias
   - Calcular média (μ) e desvio padrão (σ)
   - Calcular UCL e LCL
   - Armazenar baseline

2. Detecção Diária (Automática)
   ↓
   Para cada município e flag:
   - Contar casos do dia (Xᵢ)
   - Verificar se Xᵢ > UCL ou Xᵢ < LCL
   - Se sim → registrar anomalia imediatamente

3. Análise de Anomalias (Epidemiologista)
   ↓
   - Revisar anomalias detectadas
   - Correlacionar com contexto clínico
   - Validar se é surto real
   - Acionar resposta se necessário
```

---

## 5. Exemplo Prático

### Cenário: Monitoramento de Dengue em São Paulo

**Baseline Calculado:**
- Período: Últimos 60 dias
- μ = 15 casos/dia (média histórica)
- σ = 3.3 casos/dia (desvio padrão)
- UCL = 25 casos/dia
- LCL = 5 casos/dia

**Série Temporal:**

| Dia | Casos (Xᵢ) | Status | Interpretação |
|-----|------------|--------|---------------|
| 1   | 14         | ✅ Normal | Dentro de [5, 25] |
| 2   | 18         | ✅ Normal | Dentro de [5, 25] |
| 3   | 12         | ✅ Normal | Dentro de [5, 25] |
| 4   | 16         | ✅ Normal | Dentro de [5, 25] |
| 5   | 28         | 🚨 **ANOMALIA** | Xᵢ > UCL (28 > 25) |
| 6   | 3          | 🚨 **ANOMALIA** | Xᵢ < LCL (3 < 5) |

**Interpretação:**
- **Dia 5:** Pico súbito (28 casos) ultrapassou UCL → possível início de surto
- **Dia 6:** Queda abrupta (3 casos) abaixo de LCL → possível sub-notificação ou feriado

**Ação:** Sistema registra anomalias e notifica equipe de vigilância epidemiológica para investigação.

---

## 6. Integração com Pipeline Atual

### Dados Utilizados
Shewhart utilizará dados já coletados pela pipeline atual:

- **`ObservationSummary.DataColeta`** → timestamp para agregação diária
- **`ObservationSummary.CodigoMunicipioIBGE`** → agrupamento por região
- **`ObservationSummary.Flags`** → contagem de casos por tipo (ex: "DENGUE", "TROMBOCITOPENIA")

### Agregação
Para cada combinação de (Município, Flag, Data):
```
Casos do dia = COUNT(ObservationSummary) 
               WHERE CodigoMunicipioIBGE = X 
               AND DataColeta = Y
               AND Flags CONTAINS 'DENGUE'
```

---

## 7. Requisitos de Dados para Simulação

### 7.1. Período Mínimo de Histórico

| Cenário | Período Mínimo | Período Recomendado | Justificativa |
|---------|----------------|---------------------|---------------|
| **Desenvolvimento/Testes** | 30 dias | 60 dias (2 meses) | Baseline estatisticamente confiável |
| **Produção Inicial** | 60 dias | 90 dias (3 meses) | Maior robustez, inclui padrões semanais |

**Recomendação para simulação inicial:** **60 dias (2 meses)**
- Suficiente para μ e σ confiáveis
- Volume de dados gerenciável
- **3x mais rápido que CUSUM** (que requer 180 dias)

### 7.2. Volume Mínimo de Dados

#### **Por Município e Flag Clínica**

| Métrica | Valor Mínimo | Valor Recomendado |
|---------|--------------|-------------------|
| **Casos/dia (média)** | 3 casos/dia | 10-15 casos/dia |
| **Total no período baseline** | 90 casos | 600 casos |
| **Dias com zero casos** | < 20% | < 5% |

#### **Estrutura de Dados Simulados**

**Distribuição Sugerida para 60 dias:**
- **Flags clínicas:** 3-5 diferentes (ex: DENGUE, TROMBOCITOPENIA, LEUCOPENIA)
- **Municípios:** 5-10 municípios de portes variados
- **Registros totais:** 20.000 - 50.000 observações

### 7.3. Padrões a Simular

#### **Comportamento Normal (80% dos dados)**
```
Distribuição: Normal(μ, σ) ou Poisson(λ = μ)
Variação diária: ±30% da média
Padrão semanal: segunda > quinta > fim de semana (opcional)
```

#### **Surtos Abruptos (20% dos dados)**
```
Início: dia 45-55
Duração: 1-3 dias (surto súbito)
Padrão: pico de 150-250% sobre μ (ex: μ=15 → pico=40)
Objetivo: testar detecção imediata do Shewhart
```

---

## 8. Comparação: Shewhart vs CUSUM

### 8.1. Warm-up e Requisitos

| Aspecto | Shewhart | CUSUM | Vencedor |
|---------|----------|-------|----------|
| **Período mínimo** | 30-60 dias | 90-180 dias | ✅ Shewhart |
| **Dados necessários** | μ + σ | μ + histórico temporal | ✅ Shewhart |
| **Detecção inicial** | Imediata | 7-14 dias | ✅ Shewhart |
| **Complexidade** | Baixa | Média | ✅ Shewhart |

### 8.2. Capacidade de Detecção

| Tipo de Anomalia | Shewhart | CUSUM | Melhor Para |
|------------------|----------|-------|-------------|
| **Surto súbito** (dia 1: 15 → 40 casos) | ✅ Excelente | ⚠️ Moderado | **Shewhart** |
| **Tendência gradual** (15 → 17 → 19 → 21...) | ❌ Ruim | ✅ Excelente | **CUSUM** |
| **Pico isolado** (outlier) | ✅ Detecta | ❌ Pode ignorar | **Shewhart** |

### 8.3. Por que Shewhart Primeiro?

**Vantagens para Dados Simulados:**
1. ⚡ **Warm-up 3x mais rápido:** 60 dias vs 180 dias
2. 🎯 **Detecção imediata:** não precisa "aquecer" acumuladores
3. 📊 **Mais simples:** 2 parâmetros (μ, σ) vs 3 (μ, K, H)
4. 🧪 **Facilita testes:** menos dados mock necessários

**Limitações (endereçadas futuramente):**
- ❌ Não detecta tendências graduais → **CUSUM em fase 2**
- ❌ Sensível a outliers isolados → mitigado com validação humana

---

## 9. Extensibilidade

A implementação seguirá uma **interface comum** para algoritmos de detecção temporal:

```
Interface: IAnomalyDetectionAlgorithm
  - CalculateBaseline(municipio, flag, periodoHistorico)
  - DetectAnomaly(municipio, flag, data)
  - GetParameters()
```

**Implementações:**
- **Fase 1 (Atual):** `ShewhartDetector`
- **Fase 2 (Futuro):** `CusumDetector`
- **Fase 3 (Futuro):** `EwmaDetector`, `HybridDetector`, etc.

**Estratégia Híbrida (Futuro):**
```
Sistema Completo = Shewhart (surtos súbitos) + CUSUM (tendências)
```

---

## 10. Considerações Importantes

### 10.1. Período de Warm-up

**Shewhart:**
- Mínimo: 30 dias de observação  
- Ideal: 60 dias (2 meses) para μ e σ confiáveis
- Estabilização: **imediata** (análise por ponto)

### 10.2. Sazonalidade

Doenças com padrões sazonais (ex: dengue no verão) requerem:
- Baselines sazonais (calcular μ e σ separados por trimestre)
- Ajuste periódico dos limites (mensal ou trimestral)

### 10.3. Validação Humana

Shewhart é ferramenta de **triagem**, não diagnóstico automático. Anomalias detectadas devem ser:
- Revisadas por epidemiologistas
- Correlacionadas com contexto clínico (eventos, feriados, campanhas)
- Validadas com dados adicionais

### 10.4. Municípios com Baixa Incidência

Para regiões com menos de 3 casos/dia em média:
- Considerar agregação regional (microrregião de saúde)
- Usar limites de controle ajustados (μ ± 2σ em vez de 3σ)
- Avaliar se detecção estatística é apropriada (pode preferir análise qualitativa)

### 10.5. Falsos Positivos vs Falsos Negativos

**Trade-off dos Limites de Controle:**

| Limites | Sensibilidade | Falsos Positivos | Uso Recomendado |
|---------|---------------|------------------|-----------------|
| μ ± 2σ | Alta | Muitos | Doenças graves (meningite) |
| μ ± 3σ | Balanceada | Moderados | **Padrão (dengue, gripe)** |
| μ ± 4σ | Baixa | Poucos | Doenças comuns (resfriado) |

**Padrão recomendado:** μ ± 3σ (99.7% de confiança)

---

## 11. Referências

1. **Montgomery, D. C.** (2012). "Introduction to Statistical Quality Control" (7th ed.). *Wiley*.

2. **Shewhart, W. A.** (1931). "Economic Control of Quality of Manufactured Product." *Van Nostrand*.

3. **Woodall, W. H.** (2006). "The use of control charts in health-care and public-health surveillance." *Journal of Quality Technology*, 38(2), 89-104.

4. **Benneyan, J. C., Lloyd, R. C., & Plsek, P. E.** (2003). "Statistical process control as a tool for research and healthcare improvement." *Quality and Safety in Health Care*, 12(6), 458-464.

5. **WHO** (2011). "Outbreak Surveillance and Response in Humanitarian Emergencies."
