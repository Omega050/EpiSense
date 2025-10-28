# ADR 008: Comunicação Inter-Módulos via Callback Pattern

**Status:** Obsoleta (Substituído pela ADR-010)

**Contexto:**
Com a decisão de adiar a implementação de comunicação event-driven (ADR-003 está Pendente) para evitar complexidade prematura, precisamos de uma forma de comunicação entre os módulos Ingestion e Analysis que permita:
1. Desacoplamento lógico entre os contextos
2. Simplicidade de implementação no estágio atual
3. Facilidade de migração futura para eventos

**Decisão:**
Adotaremos **callbacks injetados via Func<T, Task>** para comunicação entre módulos durante a fase inicial. O módulo Ingestion receberá um callback opcional que será executado após persistência bem-sucedida dos dados brutos:

```csharp
Func<RawHealthData, Task>? analysisCallback
```

Este callback é configurado na camada de composição (Program.cs) e permite que o módulo Analysis seja acionado sem que Ingestion conheça seus detalhes de implementação.

**Alternativas Consideradas:**

* **Chamadas Diretas entre Módulos:** Ingestion chamar diretamente métodos do Analysis. Foi descartado por criar acoplamento forte e dificultar futura separação em microsserviços.

* **Implementar Event-Driven Imediatamente:** Usar MediatR ou barramento de eventos desde o início. Foi descartado para evitar complexidade desnecessária na fase de validação das regras de negócio (ver ADR-003).

* **Implementar Fila de Processamento:** Usar uma fila em memória ou externa. Foi descartado por adicionar complexidade operacional sem benefício claro no volume atual.

**Consequências:**

**Positivas:**

1. **Desacoplamento Temporal:** Ingestion não precisa conhecer a interface do Analysis.

2. **Simplicidade:** Implementação direta sem frameworks adicionais.

3. **Testabilidade:** Fácil criar mocks do callback em testes unitários.

4. **Caminho de Migração:** Callback pode ser facilmente substituído por publicação de evento no futuro.

5. **Callback Opcional:** Ingestion funciona independentemente se há ou não callback registrado.

**Negativas:**

1. **Não é Event-Driven Real:** Execução síncrona disfarçada - se callback falhar, toda operação falha.

2. **Acoplamento na Composição:** Program.cs precisa conhecer ambos os módulos para configurar o callback.

3. **Sem Histórico de Eventos:** Não há registro persistente da comunicação entre módulos.

4. **Escalabilidade Limitada:** Callbacks in-process não escalam para múltiplas instâncias.

**Implementação:**
```csharp
// Program.cs - Composição
builder.Services.AddScoped<IngestionService>(serviceProvider =>
{
    var repository = serviceProvider.GetRequiredService<IIngestionRepository>();
    var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();
    var logger = serviceProvider.GetRequiredService<ILogger<IngestionService>>();
    var analysisService = serviceProvider.GetRequiredService<FhirAnalysisService>();
    var analysisRepository = serviceProvider.GetRequiredService<IAnalysisRepository>();

    Func<RawHealthData, Task> analysisCallback = async (rawData) =>
    {
        var fhirJson = rawData.FhirData.ToJson();
        var summary = analysisService.AnalyzeObservation(fhirJson, rawData.Id, rawData.Metadata.ReceivedAt);
        await analysisRepository.SaveAsync(summary);
    };

    return new IngestionService(repository, eventPublisher, logger, analysisCallback);
});
```

**Estratégia de Evolução:**
Este padrão será substituído por comunicação event-driven quando:
1. Sistema estiver estabilizado e regras de negócio validadas
2. Houver necessidade de múltiplos consumidores de eventos
3. Requisitos de auditoria demandarem histórico de eventos
4. Escalabilidade horizontal se tornar necessária
