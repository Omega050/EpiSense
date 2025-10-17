# ADR 009: Resiliência de Conexão PostgreSQL com Retry Policy

**Status:** Aceito

**Contexto:**
Em ambientes distribuídos com containers Docker, conexões de banco de dados podem falhar temporariamente devido a:
- Inicialização assíncrona dos serviços
- Reinicializações de containers
- Instabilidades transitórias de rede
- Manutenções programadas

O Entity Framework Core oferece suporte nativo a retry policies para aumentar a resiliência da aplicação contra falhas transitórias.

**Decisão:**
Configuraremos **retry policy automática** para todas as operações do PostgreSQL através do Entity Framework Core:

```csharp
options.UseNpgsql(dataSource, npgsqlOptions => 
{
    npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorCodesToAdd: null
    );
});
```

**Parâmetros:**
- **maxRetryCount: 3** - Até 3 tentativas após a falha inicial
- **maxRetryDelay: 5 segundos** - Intervalo máximo entre tentativas (com exponential backoff)
- **errorCodesToAdd: null** - Usa códigos de erro padrão do Npgsql para falhas transitórias

**Alternativas Consideradas:**

* **Sem Retry Policy:** Deixar aplicação falhar na primeira falha de conexão. Foi descartado por reduzir disponibilidade e forçar lógica de retry na camada de aplicação.

* **Retry Manual na Camada de Aplicação:** Implementar retry pattern manualmente em cada repository. Foi descartado por duplicação de código e pela capacidade nativa do EF Core.

* **Biblioteca Externa (Polly):** Usar biblioteca dedicada de resiliência. Foi descartado porque o retry nativo do EF Core é suficiente para o caso de uso atual e reduz dependências.

* **Retry Infinito:** Configurar tentativas ilimitadas. Foi descartado por poder mascarar problemas reais de configuração ou infraestrutura.

**Consequências:**

**Positivas:**

1. **Alta Disponibilidade:** Aplicação tolera falhas transitórias sem intervenção manual.

2. **Startup Resiliente:** Containers podem iniciar em qualquer ordem - a aplicação aguardará o PostgreSQL.

3. **Transparência:** Retry acontece automaticamente sem lógica adicional nos repositories.

4. **Exponential Backoff:** EF Core implementa backoff exponencial automaticamente, evitando sobrecarga do banco.

**Negativas:**

1. **Latência em Caso de Falha:** Operações podem demorar até ~15 segundos (1s + 2s + 5s + 5s) antes de falhar definitivamente.

2. **Mascaramento de Problemas:** Pode ocultar problemas reais de configuração durante desenvolvimento.

3. **Logs Ruidosos:** Tentativas de retry geram logs de erro intermediários que não representam falha real.

**Monitoramento:**
Para evitar mascaramento de problemas, recomenda-se:
- Monitorar logs do EF Core em nível `Warning` para identificar retries frequentes
- Alertar quando taxa de retry exceder limite aceitável
- Revisar métricas de latência de operações de banco

**Configurações por Ambiente:**
```csharp
// Development: retry mais agressivo para facilitar desenvolvimento
maxRetryCount: 5, maxRetryDelay: 10s

// Production: retry conservador para falhar rápido em problemas reais
maxRetryCount: 3, maxRetryDelay: 5s
```
