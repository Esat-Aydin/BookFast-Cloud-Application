# BookFast bounded contexts

## Context map

BookFast is split into four bounded contexts so that the platform can grow toward an integration-focused Azure architecture without coupling every concern to endpoint code.

| Context | Current ownership | Planned evolution | Primary interfaces |
| --- | --- | --- | --- |
| Reservation API | Handles write-oriented reservation flows in the API | Move into dedicated application and infrastructure layers with persistent storage and domain events | REST `/api/v1/reservations` |
| Query API | Exposes read-oriented room and reservation queries | Expand into richer consumer read models and GraphQL governance | REST `/api/v1/rooms`, GraphQL `/graphql` |
| Async integration layer | Not implemented as a runtime yet | Publish events, process them through Service Bus and Azure Functions, and integrate with downstream consumers | Integration events, queues, topics, webhooks |
| Platform and operations | Diagnostics and health run inside the API today | Add APIM, Entra ID, Key Vault, Application Insights, Log Analytics, alerts, and Azure DevOps delivery | APIM, telemetry, pipelines, Bicep |

## Reservation API

### Responsibility

Owns command-style business actions such as creating or cancelling reservations.

### Current implementation

- Minimal API endpoints under `/api/v1/reservations`
- Validation and ProblemDetails responses at the HTTP edge
- SQL-backed execution through `IBookFastCatalog` and EF Core persistence

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

- No dedicated runtime yet
- No broker or outbox pattern yet

### Planned boundary

- Reservation and availability events published to Azure Service Bus
- Azure Functions consume and process downstream integration work
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
- Bicep and Azure DevOps YAML for reproducible delivery
