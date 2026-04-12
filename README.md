# BookFast

BookFast is a portfolio project that is being evolved into a compact Azure integration platform for room reservations. The current implementation already exposes a .NET 10 minimal API, a GraphQL read surface, structured diagnostics, and a lightweight React shell. Around that runtime, the repository now carries the architecture, ADR, Bicep, and Azure DevOps scaffolding needed to grow toward Azure API Management, Azure Functions, Service Bus, Azure SQL, and full operational visibility.

## Current baseline

| Area | Current state |
| --- | --- |
| API | ASP.NET Core minimal API under `/api/v1` with OpenAPI in development, ProblemDetails, correlation-aware request logging, and health checks. |
| Query surface | GraphQL endpoint on `/graphql` using Hot Chocolate with consumer-oriented room, reservation, availability, and occupancy read models plus explicit paging, sorting, filtering, and cost guardrails. |
| Persistence | EF Core persistence on SQL Server / Azure SQL, with seeded rooms, startup migrations in development, and database-backed readiness checks. |
| Async integration | Durable SQL outbox, background dispatcher, local fake reporting consumer, Service Bus-ready publisher configuration, and Azure Functions isolated-worker consumer runtime. |
| Azure Functions consumer | `BookFast.Reporting.Functions` — isolated-worker Azure Function that consumes `reservation.created.v1` from Service Bus, upserts `ReportingReservationSyncs`, and persists idempotency state and dead-letter records. |
| Shared integration contracts | `BookFast.Integration.Contracts` — shared project with typed event records and serialization helpers used by both the API and the Functions consumer. |
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
| `src/shared/BookFast.Integration.Contracts` | Shared integration event contracts and serialization helpers |
| `src/functions/BookFast.Reporting.Functions` | Azure Functions isolated-worker consumer runtime (phase 4) |
| `src/functions/BookFast.Reporting.Functions.Tests` | Unit tests for the Functions message processor |
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

### Run the Functions consumer

The `BookFast.Reporting.Functions` project requires:
- Azure Service Bus with a topic named `bookfast.integration` and a subscription named `reporting`
- Access to the same SQL Server / Azure SQL database as the API

Copy the config template and fill in the blanks:

```powershell
Copy-Item src\functions\BookFast.Reporting.Functions\local.settings.json.template `
          src\functions\BookFast.Reporting.Functions\local.settings.json
```

Edit `local.settings.json` and set:

```json
{
  "Values": {
    "BookFastServiceBus": "<your-service-bus-connection-string>",
    "ServiceBus__TopicName": "bookfast.integration",
    "ServiceBus__SubscriptionName": "reporting"
  },
  "ConnectionStrings": {
    "BookFastDatabase": "<your-sql-database-connection-string>"
  }
}
```

Then start the Function:

```powershell
Set-Location src\functions\BookFast.Reporting.Functions
func start
```

> Azure Functions Core Tools must be installed. The `local.settings.json` file is excluded from source control.

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

## Azure DevOps pipelines

- `pipelines/azure-devops/ci.yml` validates the frontend, API, and Azure Functions projects and publishes application artifacts.
- `pipelines/azure-devops/app-deploy.yml` builds, tests, packages, and deploys the API plus `BookFast.Reporting.Functions` to existing Azure resources.
- `pipelines/azure-devops/infra-deploy.yml` remains the infrastructure what-if pipeline for the Bicep scaffold.

The recommended enterprise setup for the app deployment pipeline is:

- an ARM / workload identity Azure service connection
- an Azure DevOps **Library variable group** linked to **Azure Key Vault**
- an existing Web App for the API and an existing Function App for `BookFast.Reporting.Functions`

Create these Key Vault secrets and map them into the variable group with the same names:

- `apiSqlConnectionString`
- `apiServiceBusConnectionString`
- `functionSqlConnectionString`
- `functionServiceBusConnectionString`

Then run `pipelines/azure-devops/app-deploy.yml` with the `keyVaultVariableGroup` parameter set to that Library group name.

Recommended Azure DevOps setup:

1. Create an Azure Key Vault in the target environment.
2. Store the four runtime secrets above as Key Vault secrets.
3. In Azure DevOps, go to **Pipelines > Library**, create a variable group, and enable **Link secrets from an Azure key vault as variables**.
4. Authorize the Azure service connection against the vault and grant the pipeline permission to use the variable group.
5. Add approvals/checks on the variable group if you want stronger production release controls.

If `keyVaultVariableGroup` is left empty, the pipeline can still fall back to direct secret pipeline variables with the same four names, and if those are also omitted it leaves existing Azure app settings untouched.

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
