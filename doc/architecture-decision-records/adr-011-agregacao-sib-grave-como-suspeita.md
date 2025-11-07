# ADR-011: Agregação de SIB Grave como Suspeita

**Status:** Aceito  
**Data:** 2025-11-05  
**Contexto:** Análise e Agregação de Casos

---

## Contexto

Durante a agregação diária de casos para detecção de anomalias pelo algoritmo Shewhart, precisamos decidir como contar casos que apresentam a flag `SIB_GRAVE`.

Um paciente com `SIB_GRAVE` apresenta:
- Neutrofilia (> 7.500/μL) **E**
- Desvio à esquerda (bastonetes > 500/μL ou > 10%)

Enquanto um paciente com `SIB_SUSPEITA` apresenta:
- Leucocitose (> 11.000/μL) **E**
- Neutrofilia (> 7.500/μL)

Na prática, **um caso grave é também um caso suspeito** do ponto de vista epidemiológico, pois ambos indicam síndrome de infecção bacteriana.

## Decisão

**Casos com `SIB_GRAVE` serão contabilizados APENAS como `SIB_SUSPEITA` na agregação diária (`DailyCaseAggregation`).**

Ou seja:
- ✅ Um paciente com flag `SIB_SUSPEITA` → conta 1 caso em `SIB_SUSPEITA`
- ✅ Um paciente com flag `SIB_GRAVE` → conta 1 caso em `SIB_SUSPEITA` (simplificado)
- ✅ Um paciente com ambas flags `[SIB_SUSPEITA, SIB_GRAVE]` → conta 1 caso em `SIB_SUSPEITA` (deduplica)

**NÃO será criada agregação separada para `SIB_GRAVE`** no cache diário.

## Justificativa

### Vantagens:
1. **Simplicidade Estatística:** O algoritmo Shewhart analisa uma única série temporal por município (SIB_SUSPEITA), sem precisar lidar com múltiplas séries sobrepostas.

2. **Sensibilidade Epidemiológica:** Para detecção de surtos, o importante é capturar **qualquer evidência de infecção bacteriana**, independente da gravidade.

3. **Evita Dupla Contagem:** Um mesmo paciente não será contado em duas séries temporais diferentes, o que poderia inflar artificialmente as contagens.

4. **Alinhamento Clínico:** SIB_GRAVE é um subconjunto/agravamento de SIB_SUSPEITA, não uma condição mutuamente exclusiva.

### Desvantagens (Consideradas e Aceitas):
- Perda de granularidade sobre a gravidade dos casos na série temporal agregada.
- Se no futuro for necessário analisar apenas casos graves, será preciso voltar aos dados brutos (`ObservationSummary`).

## Consequências

### No Código:
- `AggregationService` consolidará todas as flags clínicas (`SIB_SUSPEITA` e `SIB_GRAVE`) em uma única flag `SIB_SUSPEITA` antes da agregação.
- A tabela `daily_case_aggregations` terá apenas registros com `flag = 'SIB_SUSPEITA'`.

### Nos Dados Brutos:
- A tabela `observation_summaries` **continuará armazenando todas as flags originais** (incluindo `SIB_GRAVE`).
- Análises detalhadas sobre gravidade ainda são possíveis consultando os dados brutos.

### No Algoritmo Shewhart:
- Analisará apenas a série temporal de `SIB_SUSPEITA` por município.
- Anomalias detectadas representam aumento em **qualquer tipo de SIB** (suspeita ou grave).

## Alternativas Consideradas

### Alternativa 1: Manter agregações separadas
- **Rejeitada:** Criaria complexidade desnecessária com séries temporais correlacionadas.

### Alternativa 2: Contar casos graves como 2 (um em cada série)
- **Rejeitada:** Inflaria artificialmente as contagens e violaria o princípio de "um paciente = um caso".

### Alternativa 3: Agregar apenas SIB_GRAVE
- **Rejeitada:** Perderia casos suspeitos leves que também são importantes para detecção precoce de surtos.

## Referências
- `ClinicalFlags.Clinical` - Definição das flags clínicas
- `AggregationService` - Implementação da lógica de agregação
- `DailyCaseAggregation` - Entidade de cache diário
- ADR-008: Comunicação Inter-módulos via Callback

---

**Decisão tomada por:** Equipe de Desenvolvimento  
**Aprovado por:** Arquiteto de Software
