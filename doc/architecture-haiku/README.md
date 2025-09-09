# EpiSense - Architecture Haiku (Versão Aprimorada)

## Objetivo Principal
Transformar o fluxo contínuo de hemogramas em **inteligência epidemiológica acionável**, detectando padrões coletivos para antecipar respostas a crises de saúde pública como surtos virais, bacterianos ou eventos ambientais.

## Proposta de Valor
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
1.  **Segurança:** A proteção dos dados de saúde é uma restrição inegociável e, portanto, uma prioridade máxima.
2.  **Confiabilidade:** A precisão dos alertas é a missão crítica. O sistema deve ser confiável para que as decisões tomadas com base nele sejam seguras.
3.  **Reusabilidade:** O sistema deve ser extensível para outros cenários de detecção.
4.  **Escalabilidade:** A arquitetura deve ser capaz de suportar um volume crescente de exames de múltiplas fontes sem degradação da performance.
5.  **Disponibilidade:** A natureza de tempo real do sistema exige que os componentes de recepção e análise estejam sempre operacionais.

## Decisões de Design de Alto Nível
* **Arquitetura de Monólito Modular:** O sistema é projetado como um conjunto de **módulos logicamente independentes** (Análise, Alertas, API) dentro de uma **única aplicação implantável (deploy monolítico)**. A comunicação entre os módulos é desacoplada para garantir alta coesão e manutenibilidade.
* **Plataforma Backend:** Adoção de C# e .NET para alta performance e aproveitamento da maturidade do ecossistema para segurança e integração.
* **Persistência de Dados:** Uso do MongoDB com seu driver nativo, aplicando um padrão de **banco de dados por módulo** (lógico) para garantir a autonomia dos dados de cada contexto.
* **Comunicação Externa:** Exposição de funcionalidades via API REST e envio de notificações push através do Firebase Cloud Messaging (FCM).