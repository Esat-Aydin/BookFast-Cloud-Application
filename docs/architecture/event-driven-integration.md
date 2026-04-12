# BookFast event-driven integration flow

## Scope

Phase 4 completes the async integration tier by adding a real Azure Functions isolated-worker consumer runtime that receives events from Azure Service Bus and populates the `ReportingReservationSyncs` projection.

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
        +--> InMemory transport (local default) --> local fake reporting consumer (phase-3 fixture only)
        |
        \--> Azure Service Bus topic 'bookfast.integration'
                |
                v
        Azure Functions isolated worker (BookFast.Reporting.Functions)
        - Service Bus trigger on subscription 'reporting'
        - Idempotency check: IntegrationConsumerStates
        - Room lookup from Rooms table
        - Upsert: ReportingReservationSyncs
        - Terminal failure: IntegrationConsumerDeadLetters
```

## What is implemented today

1. **Durable outbox**  
   Reservation creation writes business data and integration messages in the same database transaction.

2. **At-least-once publication**  
   Outbox messages stay pending until publication succeeds. If publication fails, the dispatcher retries and eventually dead-letters the outbox message when retries are exhausted.

3. **Shared integration contracts**  
   `BookFast.Integration.Contracts` contains typed event records (`ReservationCreatedIntegrationEvent`, `RoomAvailabilityChangedIntegrationEvent`) and `IntegrationEventJsonSerializer` used by both the API and the Functions consumer.

4. **Real Azure Functions consumer runtime**  
   `BookFast.Reporting.Functions` is an isolated-worker Azure Functions app that:
   - receives `reservation.created.v1` from Service Bus via a topic trigger
   - resolves room metadata from the shared SQL database
   - upserts `ReportingReservationSyncs` (the reporting projection)
   - records processed message IDs in `IntegrationConsumerStates` for idempotency
   - records terminal failures in `IntegrationConsumerDeadLetters` for diagnosis

5. **Service Bus-ready transport on the API side**  
   The runtime can switch from the local in-memory transport to Azure Service Bus by setting:
   - `Eventing:Mode=ServiceBus`
   - `ConnectionStrings:BookFastServiceBus=<connection string>`

6. **Local in-memory fake consumer still available**  
   The phase-3 local `ReportingReservationIntegrationConsumer` remains in the API codebase. It is active when `Eventing:Mode=InMemory` (the development default). It does not run in the Service Bus / Functions path.

## Message processing guarantees

| Concern | How it is handled |
| --- | --- |
| Duplicate delivery | `IntegrationConsumerStates` keyed on `(ConsumerName, MessageId)`. First-wins with a concurrent-safe unique constraint catch. |
| Missing room | Written to `IntegrationConsumerDeadLetters`. Service Bus message is completed (not sent to SB DLQ). |
| Repeated dead-letter redelivery | Dead-letter record is updated with the new delivery count; no duplicate rows. |
| Unknown event subject | Completed without processing; no consumer state recorded. |
| Auto-complete | Disabled globally via `host.json` (`autoCompleteMessages: false`); the function calls `CompleteMessageAsync` explicitly after each outcome. |

## Current event catalogue

- `reservation.created.v1` — consumed by `ReportingReservationFunction` (phase 4) and the local fake consumer
- `room.availability.changed.v1` — published by the outbox; no downstream consumer implemented yet

## Local development paths

### InMemory mode (development default)

Start the API without any extra configuration. The local fake reporting consumer runs in-process. No Service Bus or Azure Functions are needed. Useful for rapid API development and local integration testing.

### Service Bus + Functions mode (end-to-end test)

1. Set `Eventing__Mode=ServiceBus` and `ConnectionStrings__BookFastServiceBus=<connection string>` for the API.
2. Configure `src/functions/BookFast.Reporting.Functions/local.settings.json` from the `.template` file.
3. Start the API with `dotnet run`.
4. Start the Functions app with `func start` from `src/functions/BookFast.Reporting.Functions/`.
5. POST a reservation via the API. The outbox dispatcher publishes to Service Bus; the Functions app picks it up and upserts the reporting projection.

## What remains for later phases

- Terraform rollout for the Function App, hosting plan, and Service Bus topology
- APIM front-ending the API
- Additional event types consumed by Functions (e.g. `room.availability.changed.v1`)
- Application Insights telemetry wired into the Functions runtime

## Why the outbox matters here

Without an outbox, reservation storage and message publication would be a dual-write problem:

- SQL succeeds but publish fails -> downstream systems miss the reservation event
- publish succeeds but SQL fails -> downstream systems see a reservation that does not exist

The durable outbox removes that gap by storing the integration message together with the reservation write.

## Why Service Bus matters here

Service Bus is the distribution boundary between the synchronous API and the async consumer runtime:

- the API only publishes one event per occurrence
- downstream consumers process independently with their own scaling, retry, and dead-letter policies
- Azure Functions can be deployed, scaled, and operated without touching the API codebase
