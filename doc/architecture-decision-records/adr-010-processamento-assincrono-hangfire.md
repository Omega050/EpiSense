# ADR 010: Processamento Assíncrono e Jobs Agendados com Hangfire

**Status:** Aceito

**Contexto:**
O sistema EpiSense possui requisitos de processamento que não podem bloquear a API de ingestão:

1. **Análise de Observações FHIR:** Após ingestão no MongoDB, dados precisam ser analisados para extração de flags clínicas e persistência no PostgreSQL. Este processamento pode demorar centenas de milissegundos e não deve impactar o tempo de resposta da API.

2. **Agregações Diárias:** Para otimizar algoritmos de detecção de anomalias (Shewhart, CUSUM), é necessário pré-calcular contagens diárias de casos por município e flag clínica, executando em horário de baixa demanda.

3. **Detecção de Anomalias:** Algoritmos estatísticos devem rodar periodicamente sobre dados agregados para identificar surtos epidemiológicos.

4. **Observabilidade:** O sistema necessita de mecanismos para monitorar, rastrear e diagnosticar jobs em execução, incluindo métricas de performance e histórico de falhas.

A arquitetura atual (ADR-008) usa callbacks síncronos, o que compromete a disponibilidade da API, não suporta jobs recorrentes e oferece visibilidade limitada do processamento em background.

**Decisão:**
Adotaremos **Hangfire** como solução unificada de processamento assíncrono, agendamento de jobs e observabilidade. O Hangfire será configurado com:

- **Storage:** PostgreSQL (compartilhando o banco do contexto Analysis, conforme ADR-006)
- **Workers:** 5 workers paralelos para processamento concorrente
- **Dashboard Web:** Interface de monitoramento em `/hangfire` com autenticação
- **Retry Policy:** 3 tentativas automáticas com backoff exponencial (60s, 300s, 900s)

**Tipos de Jobs Implementados:**

1. **Fire-and-Forget (Análise sob Demanda):** Enfileirados após cada ingestão FHIR para processamento imediato em background.

2. **Recurring Jobs (Processamento Batch):** Executados em horários definidos via Cron expressions:
   - Agregações diárias (2h UTC)
   - Detecção de anomalias (3h UTC)

**Alternativas Consideradas:**

* **Quartz.NET:** Scheduler robusto e maduro para .NET. Foi descartado por maior complexidade de configuração, ausência de dashboard integrado e menor adoção na comunidade comparado ao Hangfire.

* **Azure Functions / AWS Lambda:** Soluções serverless para processamento assíncrono. Foram descartadas por adicionar dependência de cloud provider, complexidade no deployment local e custos operacionais em cenários de alto volume.

* **Background Services do .NET (`IHostedService`):** Solução nativa do framework. Foi descartada por não oferecer persistência de estado, retry automático, interface de monitoramento ou suporte nativo a jobs recorrentes.

* **RabbitMQ/Kafka + Consumers:** Arquitetura event-driven completa. Foi descartada por adicionar complexidade operacional prematura (ver ADR-003 Pendente). Permanece como opção para evolução futura quando volume justificar.

* **MediatR com Handlers:** Biblioteca in-process para CQRS/mediator pattern. Foi descartada por não suportar jobs recorrentes, agendamento ou persistência de estado entre restarts da aplicação.

**Consequências:**

* **Positivas:**

1. **Desacoplamento Temporal:** API de ingestão responde imediatamente (~10-20ms), transferindo processamento para background sem comprometer disponibilidade.

2. **Resiliência Automática:** Retry policy com backoff exponencial (60s, 300s, 900s) tolera falhas transitórias sem intervenção manual, complementando a estratégia do ADR-009.

3. **Observabilidade Integrada:** Dashboard web em tempo real com métricas de execução, taxa de sucesso/falha, histórico persistente e identificação imediata de jobs falhando.

4. **Persistência de Estado:** Jobs enfileirados sobrevivem a restarts da aplicação, garantindo que nenhum processamento seja perdido.

5. **Escalabilidade Horizontal:** Workers podem ser distribuídos em múltiplas instâncias do mesmo servidor ou em servidores dedicados.

* **Negativas:**

1. **Tabelas Adicionais no PostgreSQL:** Hangfire cria aproximadamente 10 tabelas no schema padrão para controle interno.

2. **Overhead de Polling:** Dashboard e workers realizam polling periódico no banco (configurado para 2 segundos), gerando carga constante.

3. **Segurança do Dashboard:** Dashboard expõe informações sensíveis e precisa de autenticação/autorização robusta em produção.

**Relacionamento com Outras ADRs:**

- **Substitui ADR-008:** Callbacks síncronos são substituídos por jobs assíncronos para análises FHIR
- **Complementa ADR-009:** Retry policy do Hangfire trabalha em conjunto com retry policy do Entity Framework
- **Prepara ADR-003:** Arquitetura de jobs facilita migração futura para event-driven quando escala justificar
