# ADR-004: Event-driven integration sits beside the API

## Status

Accepted

## Context

The target platform needs reliable asynchronous integration patterns, but the current codebase is still a modular monolith. Event-driven work should be introduced without turning the API into a transport-specific orchestration layer.

## Decision

- Business writes remain inside the API boundary.
- Integration events are introduced as a separate concern after successful business writes.
- A durable SQL outbox is used so reservation writes and integration-event persistence succeed or fail together.
- Azure Service Bus is the target broker for downstream distribution in Azure environments, while the local runtime defaults to the in-memory transport.
- The current local runtime uses an in-memory transport plus a fake reporting consumer so the event flow can be exercised without Azure infrastructure.
- Azure Functions will own asynchronous processing and downstream integration handlers.
- Reliability requirements include retries, dead-letter handling, poison-message recovery, and idempotent consumers.

## Consequences

- Synchronous API responsibilities remain clear.
- The platform can evolve toward event-driven integration without prematurely splitting into multiple services.
- The producer now has at-least-once delivery semantics, so downstream consumers must remain idempotent.
- The outbox and local dead-letter records create an auditable recovery point for future replay tooling.
