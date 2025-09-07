# Documentação Arquitetural

Este diretório consolida toda a documentação arquitetural do projeto. Ele organiza os artefatos principais para facilitar o entendimento da visão do sistema, das decisões que a sustentam e das representações visuais dos componentes e suas interações.

## Architecture Haiku

A pasta `architecture-haiku` traz uma visão concisa e de alto nível da arquitetura — objetivos, restrições, atributos de qualidade e decisões que capturam a essência do sistema.

## Architectural Decision Records (ADRs)

A pasta `architecture-decision-records` registra as principais decisões arquiteturais, com contexto, alternativas e consequências. Decisões atuais:

| ADR Id | Descrição | Status |
|-------:|-----------|--------|
| [`adr-001`](architectural-decision-records/adr-001-adocao-de-csharp-e-dotnet.md) | Adoção de C# e .NET 8 para os serviços de backend. | Proposta |
| [`adr-002`](architectural-decision-records/adr-002-uso-de-mongodb-com-driver-nativo.md) | Uso de MongoDB com o driver nativo oficial em C# para persistência (sem ORM). | Proposta |
| [`adr-003`](architectural-decision-records/adr-004-servicos-autonomos-com-comunicacao-orientada-a-eventos.md) | Serviços autônomos com comunicação orientada a eventos e Database per Service. | Proposta |

## Diagramas

A pasta `diagrams` contém esboços PlantUML (por exemplo, visões de componentes e contêineres) e outras representações visuais que ilustram a arquitetura e os relacionamentos entre os componentes.

---

Manter este documento atualizado garante que todas as partes interessadas acompanhem as principais decisões e a evolução da arquitetura ao longo do tempo.

