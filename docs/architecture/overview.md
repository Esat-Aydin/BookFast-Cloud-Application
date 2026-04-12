# BookFast architecture overview

## Purpose

This document aligns the repository with the current runtime and the intended Azure target architecture. It should be read together with the bounded context document and the ADR set in `docs/decisions/`.

## Current runtime shape

BookFast currently runs as a modular monolith with one API runtime, one Azure Functions consumer, and one lightweight frontend shell.

```text
Browser / developer shell
        |
        v
React frontend (local shell)
        |
        v
BookFast API (.NET 10 minimal API)
  |- REST write and query endpoints under /api/v1
  |- GraphQL read endpoint under /graphql with consumer-driven availability and occupancy read models
  |- Health endpoints under /health, /health/live, /health/ready
  |- ProblemDetails, correlation middleware, request logging
  |- Durable outbox dispatcher for integration events
        |
        +--> Local in-memory integration transport --> local fake reporting consumer (development default)
        |
        \--> Azure Service Bus topic 'bookfast.integration'
                |
                v
        BookFast.Reporting.Functions (Azure Functions isolated worker)
          |- Service Bus trigger on subscription 'reporting'
          |- Upserts ReportingReservationSyncs
          |- Idempotency via IntegrationConsumerStates
          |- Dead-letter recording via IntegrationConsumerDeadLetters
        |
        v
SQL Server / Azure SQL persistence via EF Core (business data + outbox + local reporting sync)

## Current responsibilities

| Component | Current responsibility | Notes |
| --- | --- | --- |
| `src/api/BookFast.API` | Exposes room and reservation APIs, GraphQL reads, diagnostics, and health checks | Persistence runs through EF Core on SQL Server / Azure SQL and GraphQL now includes consumer-driven availability and occupancy overviews |
| `src/shared/BookFast.Integration.Contracts` | Typed integration event records and serialization helpers | Shared between API and Functions consumer |
| `src/functions/BookFast.Reporting.Functions` | Azure Functions isolated-worker consumer for `reservation.created.v1` | Upserts reporting projection, records idempotency and dead-letters |
| `src/frontend` | Provides a local platform shell for demos and repository orientation | Not yet a full reservation experience |
| `.github/workflows/ci.yml` | Active validation pipeline | Validates frontend, API, and Functions on GitHub Actions |
| `.github/workflows/app-deploy.yml` | Manual application CD workflow | Builds, packages, and deploys the API and Functions through GitHub OIDC plus Azure Key Vault |
| `.github/workflows/infra-deploy.yml` | Manual infrastructure workflow | Runs Terraform format, validate, plan, and optional apply |
| `infra/terraform` | Azure IaC scaffold | Establishes naming, variables, and module conventions for Terraform |

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

Provisioning: Terraform
Delivery: GitHub Actions workflows
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

- **Current runtime truth** - the running code and GitHub Actions workflows that exist today.
- **Target platform scaffolding** - the `infra/terraform` folder and deployment workflows that prepare the move to fully reproducible Terraform-based Azure delivery.

This split is intentional. It keeps the repo honest about what is implemented today, while still creating the enterprise structure needed for the next phases.
