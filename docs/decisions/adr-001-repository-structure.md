# ADR-001: Repository structure

## Status

Accepted

## Context

The repository drifted away from the documented target state. Runtime code, architecture documentation, delivery tooling, and future Azure platform concerns were mixed together without a clear ownership model. To make the project credible as an integration-platform portfolio, the repository needs a structure that separates what runs today from what is being prepared next.

## Decision

BookFast remains a monorepo, but it is organized by runtime capability and platform concern:

```text
BookFast/
|- src/
|  |- api/BookFast.API
|  |- api/BookFast.API.Tests
|  |- frontend
|  \- functions                # reserved for future Azure Functions work
|- docs/
|  |- architecture
|  |- decisions
|  \- runbooks                 # reserved for later operational documentation
|- infra/
|  \- bicep
|- pipelines/
|  \- azure-devops
\- .github/workflows           # current active CI path
```

Key rules:

- Runtime projects stay under `src/`.
- Architecture and decision records stay under `docs/`.
- Azure provisioning assets stay under `infra/bicep`.
- Azure DevOps delivery assets stay under `pipelines/azure-devops`.
- GitHub Actions may remain active until Azure DevOps becomes the delivery system of record.

## Consequences

- Current implementation and target platform scaffolding are visible without pretending that later phases already exist.
- Runtime changes, delivery changes, and architecture documentation can evolve independently.
- The project is ready to grow with Azure Functions, Bicep modules, Azure DevOps YAML, and runbooks without another repository reshuffle.
