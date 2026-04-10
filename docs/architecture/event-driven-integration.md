# BookFast event-driven integration flow

## Scope

Phase 3 introduces reliable asynchronous integration without turning the BookFast API into a transport-specific orchestration layer.

## Current runtime flow

```text
POST /api/v1/reservations
        |
        v
Reservation write + outbox messages in one SQL transaction
        |
        v
Background outbox dispatcher
        |
        +--> InMemory transport (local default) --> fake reporting consumer
        |
        \--> Azure Service Bus topic (when configured)
```

## What is implemented today

1. **Durable outbox**  
   Reservation creation writes business data and integration messages in the same database transaction.

2. **At-least-once publication**  
   Outbox messages stay pending until publication succeeds. If publication fails, the dispatcher retries and eventually dead-letters the outbox message when retries are exhausted.

3. **Idempotent local consumer processing**  
   The local reporting consumer records processed message IDs so duplicate deliveries do not create duplicate reporting records.

4. **Poison message handling**  
   Consumer failures are retried locally and then written to a consumer dead-letter table for diagnosis and replay analysis.

5. **Service Bus-ready transport**  
   The runtime can switch from the local in-memory transport to Azure Service Bus by setting:
   - `Eventing:Mode=ServiceBus`
   - `ConnectionStrings:BookFastServiceBus=<connection string>`

## Current event catalogue

- `reservation.created.v1`
- `room.availability.changed.v1`

`reservation.created.v1` is currently consumed by the fake reporting consumer. `room.availability.changed.v1` is already published so the contract exists for downstream consumers even before Azure Functions are introduced.

## Why the outbox matters here

Without an outbox, reservation storage and message publication would be a dual-write problem:

- SQL succeeds but publish fails -> downstream systems miss the reservation event
- publish succeeds but SQL fails -> downstream systems see a reservation that does not exist

The durable outbox removes that gap by storing the integration message together with the reservation write.

## Why Service Bus matters here

Service Bus is the distribution boundary between the synchronous API and future asynchronous consumers:

- the API only publishes one event
- downstream consumers can process independently
- retries and dead-letter handling are no longer bolted onto endpoint code
- Azure Functions can later consume the same contracts without redesigning the producer

## Current local fake consumer

The local runtime includes a fake downstream reporting consumer. It simulates an external subscriber by maintaining a `ReportingReservationSyncs` projection table that is populated from `reservation.created.v1`.

This gives the project a visible downstream effect now, while keeping the Azure Functions phase separate.
