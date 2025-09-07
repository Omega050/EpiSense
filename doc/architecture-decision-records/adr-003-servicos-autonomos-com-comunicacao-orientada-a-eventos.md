# ADR 003: Adoção de Serviços Autônomos com Comunicação via Eventos

**Status:** Proposta

**Contexto:**
O objetivo de modularizar o sistema exige que os contextos (`Análise de Dados`, `Gestão de Alertas`) sejam o mais independentes possível. O modelo anterior, onde serviços acessavam um banco de dados compartilhado (mesmo que em coleções ou bancos lógicos diferentes), criava um acoplamento implícito de dados. Isso poderia dificultar a evolução independente dos serviços e violava o princípio de que um serviço deve ser o dono exclusivo de seus dados.

**Decisão:**
1.  Adotaremos o padrão **"Database per Service"**. O `Analysis Engine` será o único responsável por seu `Banco de Análise`, e o `Alert Service` será o único responsável por seu `Banco de Alertas`. Nenhum serviço poderá acessar diretamente o banco de dados de outro.
2.  A comunicação entre o `Analysis Engine` e o `Alert Service` será feita de forma assíncrona através de um **Barramento de Eventos (Message Broker)**.
3.  O `Analysis Engine` publicará **eventos granulares e com estado completo** (padrão "Event-Carried State Transfer") para cada padrão coletivo significativo que detectar. O evento conterá todo o contexto necessário para a tomada de decisão.
4.  O `Alert Service` atuará como um "subscriber", reagindo a esses eventos de forma autônoma.

**Alternativas Consideradas:**
* **Banco de Dados Compartilhado:** Descartado por criar acoplamento de dados, dificultando a manutenção e a escalabilidade independentes de cada serviço.
* **Comunicação via API Síncrona:** Descartada por criar acoplamento temporal (o serviço chamador depende da disponibilidade do serviço chamado) e ser menos resiliente que a comunicação assíncrona via broker.
* **Eventos com Payload Resumido:** Descartado por não ser acionável. Forçaria o `Alert Service` a fazer uma chamada de volta para obter detalhes, reintroduzindo o acoplamento que a arquitetura de eventos visa eliminar.

**Consequências:**
* **Positivas:**
    * **Autonomia e Desacoplamento Reais:** Os serviços podem ser desenvolvidos, implantados, escalados e até mesmo ter sua tecnologia de persistência alterada de forma totalmente independente.
    * **Alta Resiliência:** O Message Broker atua como um buffer. Se o `Alert Service` estiver offline, os eventos são enfileirados e processados quando ele voltar, sem perda de informação.
    * **Extensibilidade:** Novos serviços consumidores podem ser adicionados para "ouvir" os eventos de análise sem exigir nenhuma modificação no `Analysis Engine`.

* **Negativas:**
    * **Aumento da Complexidade de Infraestrutura:** Introduz um novo componente crítico no sistema (o Message Broker), que precisa ser gerenciado, monitorado e mantido.
    * **Consistência Eventual:** A comunicação assíncrona implica que o sistema opera sob um modelo de consistência eventual. Para este caso de uso, é perfeitamente aceitável.
