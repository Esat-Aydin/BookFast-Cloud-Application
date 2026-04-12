# BookFast bounded contexts

## Context map

BookFast is split into four bounded contexts so that the platform can grow toward an integration-focused Azure architecture without coupling every concern to endpoint code.

| Context | Current ownership | Planned evolution | Primary interfaces |
| --- | --- | --- | --- |
| Reservation API | Handles write-oriented reservation flows in the API | Move into dedicated application and infrastructure layers with persistent storage and domain events | REST `/api/v1/reservations` |
| Query API | Exposes read-oriented room and reservation queries | Expand into richer consumer read models and GraphQL governance | REST `/api/v1/rooms`, GraphQL `/graphql` |
| Async integration layer | Publishes durable integration events, keeps a local fake consumer for InMemory mode, and runs a real Azure Functions reporting consumer for Service Bus mode | Expand additional consumers and operational automation around the async tier | Integration events, queues, topics, webhooks |
| Platform and operations | Diagnostics and health run inside the API today | Add APIM, Entra ID, Key Vault, Application Insights, Log Analytics, alerts, and GitHub Actions delivery | APIM, telemetry, workflows, Terraform |

## Reservation API

### Responsibility

Owns command-style business actions such as creating or cancelling reservations.

### Current implementation

- Minimal API endpoints under `/api/v1/reservations`
- Validation and ProblemDetails responses at the HTTP edge
- SQL-backed execution through `IBookFastCatalog` and EF Core persistence
- Durable outbox messages written in the same transaction as reservation creation

### Planned boundary

- Commands and handlers in the application layer
- Persistence through Azure SQL
- Event publication after successful writes

## Query API

### Responsibility

Owns consumer-facing read models for rooms, availability, and reservations.

### Current implementation

- REST endpoints under `/api/v1/rooms`
- GraphQL endpoint on `/graphql`
- Query guards for paging, time ranges, minimum capacity, sort/filter controls, and cost limits
- Consumer-oriented availability and occupancy read models for richer query aggregation

### Planned boundary

- GraphQL stays read-focused
- Consumer-specific projections and aggregation models can evolve independently from write contracts
- API versioning and query governance stay explicit

## Async integration layer

### Responsibility

Owns asynchronous distribution of business events and background integration work.

### Current implementation

- Durable outbox persisted in the BookFast SQL database
- Background outbox dispatcher inside the API runtime
- In-memory local publisher with fake reporting consumer for downstream flow validation
- Azure Service Bus publisher in the API runtime plus `BookFast.Reporting.Functions` as the reporting consumer runtime
- Shared integration contracts used by both producer and Azure Functions consumer

### Planned boundary

- Additional event types consumed beyond `reservation.created.v1`
- More than one Azure Functions consumer per downstream capability when the platform grows
- Retry, dead-letter, poison-message handling, and idempotency become first-class concerns

## Platform and operations

### Responsibility

Owns cross-cutting platform concerns that should not be embedded in business endpoints.

### Current implementation

- Correlation middleware
- Structured request logging
- ProblemDetails enrichment
- Health endpoints

### Planned boundary

- Azure API Management for consumer exposure and governance
- Entra ID, Managed Identity, and Key Vault for identity and secrets
- Application Insights, Log Analytics, alerts, dashboards, and runbooks for operations
- Terraform and GitHub Actions for reproducible delivery
