# EpiSense - Architecture Haiku (Versão Aprimorada)

## Objetivo Principal

Transformar o fluxo contínuo de hemogramas em **inteligência epidemiológica acionável**, detectando padrões coletivos para antecipar respostas a crises de saúde pública como surtos virais, bacterianos ou eventos ambientais.

## Proposta de Valor

O poder do EpiSense não está em analisar um único exame, mas em identificar o sinal fraco de uma crise iminente a partir de dados populacionais em tempo real.

* **Sinalização Preditiva:** Ir além do alerta individual para identificar o início de um evento de saúde coletiva.
* **Contexto Geográfico:** Correlacionar anomalias laboratoriais com regiões específicas (municípios, bairros), permitindo ações de vigilância focadas.
* **Cenários de Detecção:**
* 📈 **Aumento de Leucócitos** em uma área → Suspeita de surto bacteriano local.
* � **Análise laboratorial** inteligente de dados FHIR.
* 🚨 **Detecção temporal de surtos** com algoritmos de controle estatístico.
* 📉 **Leucocitose e Neutrofilia** em múltiplos exames → Alerta precoce de infecções bacterianas (SIB).
* 🗺️ **Geolocalização** de alertas por município.

## Requisitos Chave

1. **Recepção Assíncrona de Dados:** Ingerir e processar hemogramas (recurso `Observation` FHIR R4) de forma contínua e escalável via mecanismo de `subscription`.
2. **Análise Epidemiológica:** Identificar **sinais de alerta**, abrangendo desde desvios individuais até os padrões populacionais complexos descritos acima, utilizando janelas de tempo deslizantes.
3. **Comunicação Acionável:** Notificar gestores com **informação contextualizada** (o que, onde e qual a tendência) via App Móvel (Push) e expor os dados consolidados via API REST.

## Restrições Inegociáveis

* Uso obrigatório do padrão HL7 FHIR versão R4.
* Comunicação entre sistemas via HTTPS com autenticação mTLS (mutual TLS).
* Não armazenar dados pessoais identificáveis (PII - Personally Identifiable Information).

## Atributos de Qualidade
1. **Segurança:** Proteção de dados de saúde como restrição inegociável — anonimização/pseudonimização, criptografia em trânsito e em repouso, autenticação mTLS, controle de acesso (RBAC/least privilege), logs de auditoria e políticas de retenção/eliminação para minimizar risco de exposição de PII.
2. **Confiabilidade:** Precisão e robustez das detecções e alertas — validação de entradas, garantias de integridade e idempotência do processamento, testes automatizados (unit/integration), monitoramento de deriva de dados e mecanismos de fallback para degradação graciosa.
3. **Observabilidade:** Visibilidade end-to-end via logs estruturados (redigidos), métricas de negócio e infra, tracing distribuído e dashboards (ex.: Hangfire) que permitam verificar o fluxo e saúde dos pipelines sem expor dados sensíveis — usar agregação, hashing/redaction e métricas agregadas em vez de payloads brutos.
4. **Disponibilidade:** Alta disponibilidade por meio de retry policies, processamento assíncrono resiliente (Hangfire), workers paralelos e tolerância a falhas transitórias.
5. **Extensibilidade:** Arquitetura modular e event-driven que permite adicionar novos algoritmos, flags clínicas, contextos analíticos e tipos de eventos com baixo acoplamento.
6. **Manutenibilidade:** DDD com Bounded Contexts, separação clara de responsabilidades, cobertura de testes, documentação e observabilidade que facilitam a evolução segura do sistema.
7. **Escalabilidade:** Monólito modular projetado para crescimento (scaling vertical e otimizações) com caminho claro para decomposição em microsserviços conforme demanda.

## Decisões de Design de Alto Nível

* **Arquitetura de Monólito Modular:** O sistema é projetado como um conjunto de **módulos logicamente independentes** (Análise, Alertas, API) dentro de uma **única aplicação implantável (deploy monolítico)**. A comunicação entre os módulos é desacoplada para garantir alta coesão e manutenibilidade.
* **Plataforma Backend:** Adoção de C# e .NET para alta performance e aproveitamento da maturidade do ecossistema para segurança e integração.
* **Persistência de Dados:** Uso do MongoDB com seu driver nativo, aplicando um padrão de **banco de dados por módulo** (lógico) para garantir a autonomia dos dados de cada contexto.
* **Comunicação Externa:** Exposição de funcionalidades via API REST e envio de notificações push através do Firebase Cloud Messaging (FCM).
