# ADR-003: GraphQL is a read model

## Status

Accepted

## Context

GraphQL is already present in the codebase, but without a clear architectural boundary it could easily become an alternative write interface and dilute API governance.

## Decision

- GraphQL is used for read aggregation and consumer flexibility.
- REST remains the command surface for mutations.
- Query guardrails stay mandatory: paging limits, validation of query arguments, and execution-cost limits.
- Consumer-specific read models may be added to GraphQL when they reduce round-trips or improve consumer autonomy.

## Consequences

- GraphQL can expand safely without destabilizing write flows.
- Consumers get flexible reads while the platform keeps a single authoritative mutation surface.
- Security and authorization can later be applied at field and query level without redesigning the write model.
