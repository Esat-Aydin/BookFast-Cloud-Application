# ADR-002: API style and versioning

## Status

Accepted

## Context

BookFast needs a stable integration contract while the internal architecture is still evolving. Consumers need predictable mutation semantics, explicit error contracts, and a clear strategy for future breaking changes.

## Decision

- REST remains the primary write surface for business mutations.
- Versioned REST endpoints stay under `/api/v1`.
- GraphQL is positioned as a read-oriented consumer surface, not as the primary mutation contract.
- ProblemDetails responses must include trace and correlation context so operational diagnostics remain tied to consumer-visible failures.
- Breaking changes will be introduced through a new versioned contract instead of modifying `/api/v1` in place.

## Consequences

- Consumers get stable write semantics and predictable HTTP behavior.
- Read-side flexibility can grow through GraphQL without weakening mutation governance.
- API Management version sets and revisions can be layered onto this contract model later without rethinking the consumer model.
