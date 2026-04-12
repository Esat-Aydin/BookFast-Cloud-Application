# BookFast

BookFast is a hands-on learning project that I use to design, build, and evolve a compact Azure-oriented integration platform around a room reservation domain. The business scope is intentionally simple so the technical design stays visible: versioned APIs, GraphQL reads, asynchronous integration, Azure Functions, SQL persistence, delivery automation, and platform documentation all live in one repository.

## Why this project exists

- Practice end-to-end cloud integration design on a realistic but manageable domain
- Show how synchronous APIs, event-driven messaging, serverless processing, and operational concerns fit together
- Keep the scope small enough to understand quickly, while still demonstrating meaningful architectural decisions

BookFast is intentionally a living learning project. Some layers are already implemented and runnable today, while other Azure platform concerns continue to evolve through the repository structure, workflows, and architecture docs.

## Architecture at a glance

```text
Browser / local shell
        |
        v
React frontend
        |
        v
BookFast API (.NET 10 minimal API)
  |- REST contract under /api/v1
  |- GraphQL read surface under /graphql
  |- ProblemDetails, correlation, request logging, health checks
  |- EF Core persistence to SQL Server / Azure SQL
  |- Durable outbox for integration events
        |
        +--> Local in-memory transport (development default)
        |
        \--> Azure Service Bus
                |
                v
        BookFast.Reporting.Functions
          |- Azure Functions isolated worker
          |- Service Bus trigger
          |- Idempotent projection updates
          |- Dead-letter recording for failed processing
```

## What the project demonstrates today

| Area | What BookFast currently shows |
| --- | --- |
| API runtime | ASP.NET Core minimal API with versioned REST under `/api/v1`, OpenAPI in development, stable error contracts via ProblemDetails, and health endpoints |
| Query layer | GraphQL on `/graphql` using Hot Chocolate with filtering, sorting, paging, and execution-cost guardrails for read-oriented consumer scenarios |
| Data and consistency | EF Core persistence on SQL Server / Azure SQL, plus a durable outbox pattern to keep business writes and integration messages aligned |
| Async integration | Shared event contracts, publisher abstractions, local fake consumers, and a Service Bus-ready transport boundary |
| Serverless processing | An Azure Functions isolated-worker consumer that processes reservation events, updates reporting projections, and records idempotency/dead-letter state |
| Diagnostics and governance | Correlation-aware request logging, health checks, architecture docs, ADRs, and API governance guidance |
| Delivery and platform automation | GitHub Actions workflows for validation and deployment, plus Infrastructure as Code through Terraform scaffolding for Azure rollout |

## Current technical direction

The repository already contains the core runtime pieces for API, data, and asynchronous processing. Around that runtime, the project is being shaped toward a broader Azure platform model with:

- Azure API Management as the front door for internal and external consumers
- Azure Service Bus as the distribution boundary for asynchronous integration
- Azure Functions for isolated downstream processing
- Application Insights, Log Analytics, and Azure Monitor for operational visibility
- Infrastructure as code through Terraform, combined with repeatable delivery workflows for consistent rollout

That makes BookFast useful both as a runnable application and as a compact architecture case study.

## Repository guide

| Path | Purpose |
| --- | --- |
| `src\api\BookFast.API` | Main API runtime with REST, GraphQL, health, diagnostics, and event publication |
| `src\shared\BookFast.Integration.Contracts` | Shared integration event contracts and serialization helpers |
| `src\functions\BookFast.Reporting.Functions` | Azure Functions isolated-worker consumer for reservation events |
| `src\functions\BookFast.Reporting.Functions.Tests` | Tests for the Functions message-processing path |
| `src\frontend` | Lightweight React shell for local demos and repository orientation |
| `docs\architecture` | Architecture overview, bounded contexts, and event-driven flow documentation |
| `docs\api` | API governance and GraphQL guidance |
| `docs\decisions` | Architecture decision records (ADRs) |
| `infra\terraform` | Current Terraform scaffold for Azure infrastructure rollout |
| `.github\workflows` | CI and deployment workflows |

## Local development

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker Desktop
- Azure Functions Core Tools (only needed when running the Functions app locally)
- SQL Server LocalDB or another SQL Server / Azure SQL connection string

### Run the API

```powershell
Set-Location src\api\BookFast.API
dotnet run
```

The development profile targets SQL Server LocalDB by default. Override `ConnectionStrings__BookFastDatabase` when you want to use another SQL Server or Azure SQL instance.

The API is available at `http://localhost:5096` by default:

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

### Run the Functions consumer

This is optional for local end-to-end testing of the asynchronous path.

```powershell
Set-Location src\functions\BookFast.Reporting.Functions
func start
```

The Functions app expects:

- an Azure Service Bus topic named `bookfast.integration`
- a subscription named `reporting`
- access to the same SQL Server / Azure SQL database as the API

Use `local.settings.json.template` in that folder as the starting point for local configuration.

### Run the local container flow

```powershell
docker compose up --build
```

This starts SQL Server, the API, and the frontend for a full local shell.

## Delivery and infrastructure

- `.github/workflows/ci.yml` validates the frontend, API, and Functions projects
- `.github/workflows/app-deploy.yml` builds and deploys the application components
- `.github/workflows/infra-deploy.yml` validates and plans infrastructure changes
- `infra/terraform` holds the current Azure infrastructure scaffold

Infrastructure as Code is part of the project through Terraform. The current scaffold captures the Azure rollout direction and keeps environment setup repeatable as the platform evolves.

Detailed environment setup and rollout guidance lives in the workflow files and the documentation under `docs/`.

## Documentation

- Architecture overview: `docs\architecture\overview.md`
- Event-driven integration flow: `docs\architecture\event-driven-integration.md`
- API governance: `docs\api\governance.md`
- GraphQL guide: `docs\api\graphql.md`
- ADRs: `docs\decisions\`

## Source control conventions

- `main`: hardened baseline
- `develop`: integration branch
- `feature/<name>`: feature work branched from `develop`

Use Conventional Commits for local history and pull requests.
