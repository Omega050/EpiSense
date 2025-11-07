# Documentação Arquitetural

Este diretório consolida toda a documentação arquitetural do projeto. Ele organiza os artefatos principais para facilitar o entendimento da visão do sistema, das decisões que a sustentam e das representações visuais dos componentes e suas interações.

## Architecture Haiku

A pasta `architecture-haiku` traz uma visão concisa e de alto nível da arquitetura — objetivos, restrições, atributos de qualidade e decisões que capturam a essência do sistema.

## Architectural Decision Records (ADRs)

A pasta `architecture-decision-records` registra as principais decisões arquiteturais, com contexto, alternativas e consequências. Decisões atuais:

| ADR Id | Descrição | Status |
|-------:|-----------|--------|
| [`adr-001`](architecture-decision-records\adr-001-adocao-de-csharp-e-dotnet.md) | Adoção de C# e .NET 8 para os serviços de backend. | Aceito |
| [`adr-002`](architecture-decision-records\adr-002-uso-de-mongodb-com-driver-nativo.md) | Uso de MongoDB com o driver nativo oficial em C# para persistência (sem ORM). | Substituída |
| [`adr-003`](architecture-decision-records\adr-003-servicos-autonomos-com-comunicacao-orientada-a-eventos.md) | Serviços autônomos com comunicação orientada a eventos e Database per Service. | Pendente |
| [`adr-004`](architecture-decision-records\adr-004-adocao-de-arquitetura-de-monolito-modular.md) | Adoção de Arquitetura de Monólito Modular | Aceito |
| [`adr-005`](architecture-decision-records\adr-005-validacao-fhir-no-nivel-de-api.md) | Validação FHIR no nível de API com feedback imediato ao cliente | Aceito |
| [`adr-006`](architecture-decision-records\adr-006-arquitetura-hibrida-persistencia-mongodb-postgresql.md) | Arquitetura Híbrida de Persistência com MongoDB e PostgreSQL | Aceito |
| [`adr-007`](architecture-decision-records\adr-007-padrao-repository-especifico-por-contexto.md) | Padrão Repository específico por contexto sem abstração genérica | Aceito |
| [`adr-008`](architecture-decision-records\adr-008-comunicacao-inter-modulos-via-callback.md) | Comunicação inter-módulos via Callback Pattern (estratégia transitória) | Aceito |
| [`adr-009`](architecture-decision-records\adr-009-resiliencia-postgresql-retry-policy.md) | Resiliência de conexão PostgreSQL com Retry Policy | Aceito |
| [`adr-010`](architecture-decision-records\adr-010-processamento-assincrono-hangfire.md) | Processamento assíncrono e jobs agendados com Hangfire para análises e agregações | Aceito |
| [`adr-011`](architecture-decision-records\adr-011-agregacao-sib-grave-como-suspeita.md) | Agregação de SIB Grave como Suspeita para simplificação epidemiológica | Aceito |

## Diagramas

A pasta `diagrams` contém esboços PlantUML (por exemplo, visões de componentes e contêineres) e outras representações visuais que ilustram a arquitetura e os relacionamentos entre os componentes.

---

Manter este documento atualizado garante que todas as partes interessadas acompanhem as principais decisões e a evolução da arquitetura ao longo do tempo.
