# EpiSense - Architecture Haiku (Versão Aprimorada)

## Objetivo Principal
Transformar o fluxo contínuo de hemogramas em **inteligência epidemiológica acionável**, detectando padrões coletivos para antecipar respostas a crises de saúde pública como surtos virais (Dengue), bacterianos ou eventos ambientais.

## Proposta de Valor (O Diferencial)
O poder do EpiSense não está em analisar um único exame, mas em identificar o sinal fraco de uma crise iminente a partir de dados populacionais em tempo real.

* **Sinalização Preditiva:** Ir além do alerta individual para identificar o início de um evento de saúde coletiva.
* **Contexto Geográfico:** Correlacionar anomalias laboratoriais com regiões específicas (municípios, bairros), permitindo ações de vigilância focadas.
* **Cenários de Detecção:**
    * 📈 **Aumento de Leucócitos** em uma área → Suspeita de surto bacteriano local.
    * 📉 **Queda de Plaquetas** em múltiplos exames → Alerta precoce de arboviroses (ex: Dengue).
    * 🩸 **Redução de Hemoglobina** em um grupo populacional → Indício de problemas ambientais ou nutricionais.

## Requisitos Chave 
1.  **Recepção Assíncrona de Dados:** Ingerir e processar hemogramas (recurso `Observation` FHIR R4) de forma contínua e escalável via mecanismo de `subscription`.
2.  **Análise Epidemiológica:** Identificar **sinais de alerta**, abrangendo desde desvios individuais até os padrões populacionais complexos descritos acima, utilizando janelas de tempo deslizantes.
3.  **Comunicação Acionável:** Notificar gestores com **informação contextualizada** (o que, onde e qual a tendência) via App Móvel (Push) e expor os dados consolidados via API REST.

## Restrições Inegociáveis
* Uso obrigatório do padrão HL7 FHIR versão R4.
* Comunicação entre sistemas via HTTPS com autenticação mTLS (mutual TLS).
* Não armazenar dados pessoais identificáveis (PII - Personally Identifiable Information).

## Atributos de Qualidade
1.  **Confiabilidade:** A precisão dos alertas é a missão crítica. O sistema deve ser confiável para que as decisões tomadas com base nele sejam seguras.
2.  **Segurança:** A proteção dos dados de saúde é uma restrição inegociável e, portanto, uma prioridade máxima.
3.  **Disponibilidade:** A natureza de tempo real do sistema exige que os componentes de recepção e análise estejam sempre operacionais.
4.  **Escalabilidade:** A arquitetura deve ser capaz de suportar um volume crescente de exames de múltiplas fontes sem degradação da performance.

## Decisões de Design de Alto Nível
* **Arquitetura Orientada a Serviços:** Sistema decomposto em serviços especializados e desacoplados (Receptor FHIR, Mecanismo de Análise, Serviço de Alertas) para garantir manutenibilidade e escalabilidade independentes.
* **Plataforma Backend:** Adoção de C# e .NET para alta performance e aproveitamento da maturidade do ecossistema para segurança e integração.
* **Persistência de Dados:** Uso do MongoDB com seu driver nativo para alinhar o modelo de persistência (documento) com a natureza dos dados (FHIR/JSON) e para alavancar seu poderoso Aggregation Framework nas análises coletivas.
* **Comunicação Externa:** Exposição dos alertas via API REST para o App Móvel e envio de notificações push através do Firebase Cloud Messaging (FCM).