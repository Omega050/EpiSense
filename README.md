# EpiSense

Initial scaffold focused on the backend API and documentation.

- Backend: .NET 8 minimal API in `backend/` with `/health` endpoint
- Docs: ADRs and architecture notes in `doc/`

## Quickstart

- Build backend: dotnet build backend/EpiSense.sln -c Release
- Run backend: dotnet run --project backend/src/EpiSense.Api
- Docker: docker build -t episense-api:dev -f backend/Dockerfile backend

## Backlog do Projeto

A tabela abaixo representa as próximas etapas de desenvolvimento, organizadas em épicos para construir o sistema de forma incremental.

| Épico | Tarefa | Prioridade | Status |
| :--- | :--- | :--- | :--- |
| **1. Pipeline de Ingestão** | Implementar `SubscriptionController` para receber notificações FHIR. | Alta | A Fazer |
| **1. Pipeline de Ingestão** | Implementar `IFhirClient` para comunicação real com o servidor FHIR. | Alta | A Fazer |
| **1. Pipeline de Ingestão** | Implementar o parsing do JSON FHIR em `Hemograma.FromFhirJson`. | Alta | A Fazer |
| **1. Pipeline de Ingestão** | Implementar `IRawDataRepository` com o driver do MongoDB (ADR-002). | Alta | A Fazer |
| **2. Análise e Alertas** | Desenvolver o Módulo de Análise (regras de desvio). | Média | A Fazer |
| **2. Análise e Alertas** | Desenvolver o Módulo de Alertas (entidade e serviço). | Média | A Fazer |
| **2. Análise e Alertas** | Implementar a API de Alertas (`GET /api/alerts`). | Média | A Fazer |
| **3. Qualidade e Segurança** | Implementar autenticação mTLS nos endpoints da API. | Alta | A Fazer |
| **3. Qualidade e Segurança** | Adicionar testes de integração para o fluxo de ingestão. | Média | A Fazer |
| **3. Qualidade e Segurança** | Adicionar testes unitários para entidades e serviços. | Baixa | A Fazer |
| **3. Qualidade e Segurança** | Configurar um endpoint de Health Check (`/health`). | Baixa | **Concluído** ✅ |
