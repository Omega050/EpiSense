# ADR 007: Padrão Repository Específico por Contexto

**Status:** Aceito

**Contexto:**
Com a adoção da arquitetura de monólito modular (ADR-004), cada contexto limitado (Ingestion, Analysis, Alerts) possui suas próprias necessidades de persistência. Surge a decisão de como implementar o padrão Repository: usar interfaces genéricas (`IRepository<T>`) ou interfaces específicas por contexto.

**Decisão:**
Adotaremos **repositories específicos por contexto** com interfaces dedicadas:
- `IIngestionRepository` para o contexto de Ingestion
- `IAnalysisRepository` para o contexto de Analysis
- Cada interface expõe apenas os métodos necessários para seu domínio

**Não** utilizaremos abstração genérica `IRepository<T>` ou `IGenericRepository<T>`.

**Alternativas Consideradas:**

* **Repository Genérico (`IRepository<T>`):** Implementar uma interface genérica com operações CRUD padrão reutilizável. Foi descartado porque força uma uniformidade nas operações que nem sempre se aplica aos diferentes contextos e pode levar a métodos não utilizados ou inadequados.

* **Repository por Entidade:** Criar um repository para cada entidade (ex: `IObservationSummaryRepository`, `IRawHealthDataRepository`). Foi descartado por ser muito granular e aumentar a complexidade de injeção de dependência sem benefícios claros.

**Consequências:**

**Positivas:**

1. **Adequação ao Domínio:** Cada repository expõe exatamente as operações que fazem sentido para seu contexto específico.

2. **Flexibilidade:** Permite métodos customizados sem poluir interface genérica (ex: `GetDataByStatusAsync`, `GetAnalysisByDateRangeAsync`).

3. **Clareza:** Interface autodocumentada - ao ver `IAnalysisRepository`, é claro que pertence ao contexto de Analysis.

4. **Evolução Independente:** Cada contexto pode evoluir suas necessidades de persistência sem impactar outros.

**Negativas:**

1. **Duplicação de Código:** Métodos comuns (SaveAsync, GetByIdAsync) são repetidos em cada interface.

2. **Menos Reuso:** Não há reaproveitamento de implementações base.

**Implementação:**
Cada contexto define sua própria interface de repository:

```csharp
// Contexto: Ingestion
public interface IIngestionRepository
{
    Task SaveRawDataAsync(RawHealthData data);
    Task<RawHealthData?> GetRawDataByIdAsync(string id);
    Task UpdateStatusAsync(string id, IngestionStatus status);
    Task<IEnumerable<RawHealthData>> GetDataByStatusAsync(IngestionStatus status);
}

// Contexto: Analysis
public interface IAnalysisRepository
{
    Task SaveAsync(ObservationSummary summary);
    Task<ObservationSummary?> GetByIdAsync(Guid id);
    Task<IEnumerable<ObservationSummary>> GetAllAsync();
    Task<IEnumerable<ObservationSummary>> GetByDateRangeAsync(DateTime start, DateTime end);
}
```
