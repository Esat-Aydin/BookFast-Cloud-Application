# BookFast

BookFast is a portfolio project that is being evolved into a compact Azure integration platform for room reservations. The current implementation already exposes a .NET 10 minimal API, a GraphQL read surface, structured diagnostics, and a lightweight React shell. Around that runtime, the repository now carries the architecture, ADR, Bicep, and Azure DevOps scaffolding needed to grow toward Azure API Management, Azure Functions, Service Bus, Azure SQL, and full operational visibility.

## Current baseline

| Area | Current state |
| --- | --- |
| API | ASP.NET Core minimal API under `/api/v1` with OpenAPI in development, ProblemDetails, correlation-aware request logging, and health checks. |
| Query surface | GraphQL endpoint on `/graphql` using Hot Chocolate with consumer-oriented room, reservation, availability, and occupancy read models plus explicit paging, sorting, filtering, and cost guardrails. |
| Persistence | EF Core persistence on SQL Server / Azure SQL, with seeded rooms, startup migrations in development, and database-backed readiness checks. |
| Async integration | Durable SQL outbox, background dispatcher, local fake reporting consumer, and Service Bus-ready publisher configuration. |
| Frontend | React/Vite shell for local demos, repository orientation, and future integration consumer flows. |
| Delivery | GitHub Actions CI remains active today. Azure DevOps pipeline scaffolding lives under `pipelines/azure-devops/`. |
| Infrastructure | Initial Bicep conventions and environment parameter scaffolding live under `infra/bicep/`. |

## Target platform

BookFast is being shaped toward the following target flow:

```text
Partner or internal consumer -> Azure API Management -> BookFast REST/GraphQL API -> Azure SQL
Partner or internal consumer -> Azure API Management -> Service Bus -> Azure Functions -> Downstream consumers
All runtime components -> Application Insights + Log Analytics + Azure Monitor
Provisioning -> Bicep
Delivery -> Azure DevOps YAML pipelines
```

The detailed roadmap lives in the architecture docs and ADRs under `docs/`.

## Repository guide

| Path | Purpose |
| --- | --- |
| `src/api/BookFast.API` | Current API runtime with REST, GraphQL, diagnostics, and health endpoints |
| `src/frontend` | Current frontend shell and local developer-facing UI |
| `docs/architecture` | Architecture overview and bounded context documentation |
| `docs/decisions` | Architecture decision records (ADRs) |
| `infra/bicep` | Bicep naming, parameter, and module scaffolding for Azure rollout |
| `pipelines/azure-devops` | Azure DevOps YAML pipeline scaffolding |

## Local development

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker Desktop (optional, for the compose flow)
- SQL Server LocalDB or an alternative SQL Server / Azure SQL connection string for direct `dotnet run`

### Run the API

```powershell
Set-Location src\api\BookFast.API
dotnet run
```

The development profile targets SQL Server LocalDB by default. Override `ConnectionStrings__BookFastDatabase` when you want to use another SQL Server or Azure SQL instance.

Local development uses the in-memory event transport by default. Switch to Azure Service Bus by setting:

```powershell
$env:Eventing__Mode = "ServiceBus"
$env:ConnectionStrings__BookFastServiceBus = "<service-bus-connection-string>"
```

The API is available at `http://localhost:5096` by default, with:

- REST: `http://localhost:5096/api/v1`
- GraphQL: `http://localhost:5096/graphql`
- Health: `http://localhost:5096/health`

### Run the frontend

```powershell
Set-Location src\frontend
npm install
npm run dev
```

The frontend shell is available at `http://localhost:5173`.

> Local CORS is configured for `http://localhost:3000` and `http://localhost:5173`. The frontend remains intentionally lightweight and repository-focused at this stage.

### Run the local container flow

```powershell
docker compose up --build
```

This starts:

- SQL Server on `localhost:1433`
- API on `http://localhost:5000`
- Frontend on `http://localhost:3000`

The compose flow wires the API to the SQL Server container and applies pending EF Core migrations during startup.

## Documentation

- Architecture overview: `docs/architecture/overview.md`
- Event-driven integration flow: `docs/architecture/event-driven-integration.md`
- Bounded contexts: `docs/architecture/bounded-contexts.md`
- API governance: `docs/api/governance.md`
- GraphQL consumer guide: `docs/api/graphql.md`
- ADR index: `docs/decisions/`

## Git hooks

Activate the repository hooks once after cloning:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\setup-git-hooks.ps1
```

The pre-commit hook runs the same `.NET format` check as CI for `src/api/BookFast.API/BookFast.API.csproj`.

To fix formatting locally:

```powershell
Push-Location src\api
dotnet tool restore
dotnet tool run dotnet-format -- BookFast.API\BookFast.API.csproj
Pop-Location
```

## Source control conventions

- `main`: hardened baseline
- `develop`: integration branch
- `feature/<name>`: feature work branched from `develop`

Use Conventional Commits for local history and PRs.
