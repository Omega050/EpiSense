# EpiSense - Architecture Haiku (Revisado)

## Objetivo Principal
Automatizar a recepção e análise de hemogramas em tempo real para detectar anomalias individuais e padrões coletivos, notificando gestores de saúde pública para antecipar a identificação de surtos e agravos.

## Requisitos Chave (Top 3)
1.  **Recepção Assíncrona de Dados:** Receber e processar hemogramas (recurso `Observation` FHIR R4) de forma assíncron via mecanismo de `subscription`.
2.  **Análise em Duas Camadas:** Implementar a análise de desvios individuais (valores fora da faixa) e detecção de padrões coletivos anômalos em janelas de tempo deslizantes por região geográfica.
3.  **Notificação e Consulta:** Notificar gestores via aplicativo móvel (Push) e expor os alertas gerados através de uma API REST documentada.

## Restrições Inegociáveis
* Uso obrigatório do padrão HL7 FHIR versão R4.
* Comunicação entre sistemas via HTTPS com autenticação mTLS (mutual TLS).
* Não armazenar dados pessoais identificáveis (PII - Personally Identifiable Information).

## Atributos de Qualidade (Priorizados)
1.  **Confiabilidade:** Garantir o processamento sem perdas de todos os hemogramas e a precisão dos alertas é a missão crítica do sistema.
2.  **Segurança:** A proteção dos dados de saúde é uma restrição inegociável e, portanto, uma prioridade máxima.
3.  **Escalabilidade:** A arquitetura deve ser capaz de suportar um volume crescente de exames sem degradação da performance.
4.  **Modularidade:**

## Decisões de Design de Alto Nível
* **Arquitetura Orientada a Serviços:** O sistema será decomposto em serviços especializados e desacoplados (ex: Receptor FHIR, Analisador de Dados, API de Alertas, Notificador) que se comunicam via mensageria ou APIs internas.
* **Plataforma Backend:** Adoção de Java/Kotlin com Spring Boot para agilizar o desenvolvimento da API REST e a gestão do endpoint de `subscription`, aproveitando o ecossistema maduro para integração e segurança.
* **Persistência de Dados:** Uso de um banco de dados NoSQL (ex: MongoDB) para flexibilidade com os documentos FHIR, ou um relacional (ex: PostgreSQL) com suporte a JSONB para análises estruturadas. A escolha final será validada no **Marco 3** com base na complexidade das consultas analíticas.
* **Comunicação Externa:** Exposição dos alertas via API REST para consumo externo (App Móvel) e envio de notificações push para o App Android através do Firebase Cloud Messaging (FCM).

---
