# ADR-004: Event-driven integration sits beside the API

## Status

Accepted

## Context

The target platform needs reliable asynchronous integration patterns, but the current codebase is still a modular monolith. Event-driven work should be introduced without turning the API into a transport-specific orchestration layer.

## Decision

- Business writes remain inside the API boundary.
- Integration events are introduced as a separate concern after successful business writes.
- Azure Service Bus is the default broker for downstream distribution.
- Azure Functions will own asynchronous processing and downstream integration handlers.
- Reliability requirements include retries, dead-letter handling, poison-message recovery, and idempotent consumers.

## Consequences

- Synchronous API responsibilities remain clear.
- The platform can evolve toward event-driven integration without prematurely splitting into multiple services.
- Future outbox or publish-consistency work has an explicit architectural home.
