# ADR-005: Observability starts with correlation

## Status

Accepted

## Context

BookFast already logs requests and exceptions, but the project needs a stronger operational foundation before it can credibly demonstrate APIM, messaging, and serverless integration patterns.

## Decision

- `X-Correlation-Id` remains the ingress correlation header for HTTP traffic.
- ProblemDetails responses must carry both `traceId` and `correlationId`.
- Health endpoints stay available for overall, liveness, and readiness checks.
- Future telemetry work will extend this baseline into Application Insights, Log Analytics, Azure Monitor alerts, and cross-component tracing.

## Consequences

- The project has a clear operational contract even before full Azure telemetry is added.
- API failures remain diagnosable from consumer response back to server logs.
- Later APIM, Service Bus, and Azure Functions work can propagate the same correlation model instead of inventing a new one.
