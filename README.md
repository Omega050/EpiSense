# EpiSense

**EpiSense** é um sistema inteligente de vigilância epidemiológica desenvolvido para transformar o fluxo contínuo de hemogramas em **inteligência epidemiológica acionável**. O sistema detecta padrões coletivos em dados de saúde populacional para antecipar respostas a crises de saúde pública como surtos virais, bacterianos ou eventos ambientais.

## 🎯 Proposta de Valor

O poder do EpiSense está em identificar o sinal fraco de uma crise iminente a partir de dados populacionais em tempo real, indo muito além da análise de exames individuais:

- **📈 Sinalização Preditiva:** Detecta o início de eventos de saúde coletiva antes que se tornem epidemias
- **🗺️ Contexto Geográfico:** Correlaciona anomalias laboratoriais com regiões específicas (municípios, bairros)
- **🔬 Análise Inteligente:** Processa dados FHIR com algoritmos de controle estatístico (Shewhart, CUSUM)
- **🚨 Alertas Precoces:** Identifica surtos bacterianos através de leucocitose e neutrofilia em tempo real

### Cenários de Detecção

- **Surto Bacteriano Local:** Aumento anormal de leucócitos em uma região específica
- **Infecção Bacteriana (SIB):** Leucocitose e neutrofilia em múltiplos exames
- **Mudanças Graduais:** Detecção de shifts sutis em padrões populacionais

## 🏗️ Arquitetura

EpiSense é construído como um **monólito modular** com contextos claramente delimitados:

- **Backend:** .NET 8 com arquitetura DDD (Domain-Driven Design)
- **Persistência Híbrida:** PostgreSQL (análises/agregações) + MongoDB (dados brutos)
- **Processamento Assíncrono:** Hangfire para jobs agendados e análises em background
- **Padrão FHIR R4:** Conformidade total com HL7 FHIR para interoperabilidade

Para mais detalhes, consulte a [documentação arquitetural](doc/README.md) e os [Architecture Decision Records](doc/architecture-decision-records/).

## 🚀 Quickstart

### Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose
- PostgreSQL 16+
- MongoDB 7+

### Executando Localmente

```bash
# 1. Iniciar infraestrutura (PostgreSQL, MongoDB)
docker-compose up -d

# 2. Build do backend
dotnet build backend/EpiSense.sln -c Release

# 3. Executar API
dotnet run --project backend/src/Apps/EpiSense.Api

# 4. Acessar endpoints
# - API: http://localhost:5000
# - Health: http://localhost:5000/health
# - Hangfire Dashboard: http://localhost:5000/hangfire
```

### Docker

```bash
docker build -t episense-api:dev -f backend/Dockerfile backend
docker run -p 5000:8080 episense-api:dev
```

## 📖 Documentação

- **[Architecture Haiku](doc/architecture-haiku/)** - Visão de alto nível da arquitetura
- **[ADRs](doc/architecture-decision-records/)** - Decisões arquiteturais documentadas
- **[Diagramas](doc/diagrams/)** - Diagramas C4 e fluxos de dados
- **[ROADMAP](doc/ROADMAP.md)** - Planejamento de iterações


---

**EpiSense** é um exemplo de como tecnologia e dados podem ser utilizados para proteger a saúde pública através de vigilância epidemiológica inteligente e acionável
