# ADR 003: Adoção de Serviços Autônomos com Comunicação via Eventos In-Memory

**Status:** Pendente

**Contexto:**
Após a decisão de arquitetura de **Monólito Modular** (ver ADR 004), os módulos (`Análise`, `Alertas`) precisam permanecer desacoplados logicamente. Chamada direta entre módulos (método → método) criaria acoplamento estrutural e dificultaria futura extração seletiva.

**Decisão:**

1. Comunicação entre módulos por **eventos de domínio in-memory** via mediador (ex.: MediatR).
2. Padrão **Event-Carried State Transfer**: eventos carregam o estado necessário para decisão sem "callback".
3. Cada módulo publica ou consome sem conhecer implementações concretas de outros.
4. "Database per Service" permanece lógico: nenhum módulo acessa repositório do outro.

**Alternativas Consideradas:**

- Broker Externo (RabbitMQ/Kafka) agora: descartado (complexidade infra desnecessária no estágio atual).
- Chamadas Diretas internas: descartado (acoplamento temporal e estrutural).
- Eventos mínimos + lookup cruzado: descartado (reintroduz acoplamento e dependências).

**Consequências:**

- Positivas:
  - Desacoplamento lógico e evolução independente de módulos.
  - Alta performance (in-process, sem hops de rede).
  - Facilita futura migração para broker externo (troca do mecanismo de publicação).
- Negativas:
  - Sem durabilidade de eventos fora do processo (falha derruba o fluxo).
  - Necessidade de disciplina para manter contratos de eventos versionados.

**Justificativa do Status Pendente:**

A implementação de comunicação event-driven ainda não foi realizada para evitar o escalonamento da complexidade do projeto nesta fase inicial. Atualmente, a comunicação entre os módulos de Ingestion e Analysis é feita através de callbacks diretos, o que permite validar o fluxo de dados e as regras de negócio antes de introduzir a camada adicional de abstração que a arquitetura orientada a eventos demanda. Esta decisão será reavaliada quando o sistema estiver estabilizado e houver necessidade clara de maior desacoplamento entre os módulos.
