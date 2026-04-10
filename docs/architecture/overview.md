# BookFast architecture overview

## Purpose

This document aligns the repository with the current runtime and the intended Azure target architecture. It should be read together with the bounded context document and the ADR set in `docs/decisions/`.

## Current runtime shape

BookFast currently runs as a modular monolith with one API runtime and one lightweight frontend shell.

```text
Browser / developer shell
        |
        v
React frontend (local shell)
        |
        v
BookFast API (.NET 10 minimal API)
  |- REST write and query endpoints under /api/v1
  |- GraphQL read endpoint under /graphql
  |- Health endpoints under /health, /health/live, /health/ready
  |- ProblemDetails, correlation middleware, request logging
        |
        v
InMemoryBookFastCatalog
```

## Current responsibilities

| Component | Current responsibility | Notes |
| --- | --- | --- |
| `src/api/BookFast.API` | Exposes room and reservation APIs, GraphQL reads, diagnostics, and health checks | Persistence is still in-memory |
| `src/frontend` | Provides a local platform shell for demos and repository orientation | Not yet a full reservation experience |
| `.github/workflows/ci.yml` | Active validation pipeline | GitHub Actions remains the active CI path today |
| `infra/bicep` | Azure IaC scaffold | Establishes naming, parameter, and module conventions |
| `pipelines/azure-devops` | Azure DevOps YAML scaffold | Prepared for the future delivery target |

## Target Azure platform

BookFast is being evolved toward the following target architecture:

```text
Partner / internal consumer
        |
        v
Azure API Management
   |            \
   |             \--> Service Bus --> Azure Functions --> downstream consumers
   v
BookFast API --> Azure SQL

Diagnostics and telemetry
BookFast API + Azure Functions + APIM + Service Bus
        |
        v
Application Insights + Log Analytics + Azure Monitor

Provisioning: Bicep
Delivery: Azure DevOps YAML pipelines
```

## Architectural boundaries

The solution is split into four bounded contexts:

1. **Reservation API** - command-oriented REST surface for reservation changes.
2. **Query API** - consumer-driven read surface over REST and GraphQL.
3. **Async integration layer** - event publication and serverless consumers.
4. **Platform and operations** - API gateway, identity, observability, secrets, and delivery.

See `docs/architecture/bounded-contexts.md` for the detailed ownership model.

## Delivery direction

The repository now distinguishes between:

- **Current runtime truth** - the running code and GitHub Actions validation that exist today.
- **Target platform scaffolding** - the `infra/` and `pipelines/` folders that prepare the move to Bicep and Azure DevOps.

This split is intentional. It keeps the repo honest about what is implemented today, while still creating the enterprise structure needed for the next phases.
