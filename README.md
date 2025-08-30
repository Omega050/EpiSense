# EpiSense

Initial scaffold focused on the backend API and documentation.

- Backend: .NET 8 minimal API in `backend/` with `/health` endpoint
- Docs: ADRs and architecture notes in `doc/`
- Deploy: Helm chart in `deploy/chart/episense`

## Quickstart

- Build backend: dotnet build backend/EpiSense.sln -c Release
- Run backend: dotnet run --project backend/src/EpiSense.Api
- Docker: docker build -t episense-api:dev -f backend/Dockerfile backend
- Helm: see deploy/README.md
