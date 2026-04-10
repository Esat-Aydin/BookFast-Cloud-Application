# ADR-003: GraphQL is a read model

## Status

Accepted

## Context

GraphQL is already present in the codebase, but without a clear architectural boundary it could easily become an alternative write interface and dilute API governance.

## Decision

- GraphQL is used for read aggregation and consumer flexibility.
- REST remains the command surface for mutations.
- Query guardrails stay mandatory: paging limits, validation of query arguments, explicit sort and filter controls, and execution-cost limits.
- Consumer-specific read models may be added to GraphQL when they reduce round-trips or improve consumer autonomy.
- Schema additions must be documented with example queries and backed by GraphQL contract tests.
- Phase-2 read models include combined room availability and occupancy summaries per location.

## Consequences

- GraphQL can expand safely without destabilizing write flows.
- Consumers get flexible reads while the platform keeps a single authoritative mutation surface.
- Security and authorization can later be applied at field and query level without redesigning the write model.
- Query governance remains explicit and reviewable instead of delegating the entire read contract to generic schema conventions.
