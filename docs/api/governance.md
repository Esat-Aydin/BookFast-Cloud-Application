# BookFast API governance

## Scope

This document defines the non-database phase-1 governance rules for the BookFast API surface.

## API styles

### REST

- REST under `/api/v1` is the primary mutation and operational contract.
- Write-style business actions must remain explicit HTTP operations with stable response semantics.
- ProblemDetails is the mandatory error contract for REST failures.

### GraphQL

- GraphQL on `/graphql` is a read-oriented surface.
- It is intended for consumer-driven query flexibility and aggregation.
- Mutations are not the primary contract for the platform.

## Versioning policy

- The current supported REST contract is **1.0** and remains mounted under `/api/v1`.
- Non-breaking changes may be added within the existing major version when they do not invalidate existing consumers.
- Breaking changes require a new versioned route surface, for example `/api/v2`.
- Responses from `/api/v1` expose the headers `api-selected-version` and `api-supported-versions` so consumers can reason about the current contract.

## Deprecation policy

- Deprecation is announced before removal of a versioned contract.
- A deprecated version remains available during a transition period while consumers migrate to the replacement contract.
- When deprecation is introduced in a later phase, the runtime and API gateway must expose deprecation metadata consistently through response headers and API documentation.

## Error contract

REST failures must return a ProblemDetails payload that includes:

- `status`
- `title`
- `detail`
- `instance`
- `traceId`
- `correlationId`
- `errorCode`

`errorCode` is the machine-readable contract for downstream consumers, diagnostics, and future APIM policy behavior.

## CORS policy

- CORS is configuration-driven through the `ApiCors` section.
- Development allows the local frontend origins `http://localhost:3000` and `http://localhost:5173`.
- Non-development environments must only allow explicitly approved consumer origins.
- Exposed headers include `X-Correlation-Id`, `api-selected-version`, and `api-supported-versions` so browser-based consumers can observe operational and version metadata.
