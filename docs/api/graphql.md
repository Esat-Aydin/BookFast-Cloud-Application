# BookFast GraphQL consumer guide

## Scope

GraphQL on `/graphql` is the read-oriented surface for consumer-driven query scenarios. It exists to reduce round-trips, let consumers select their own fields, and aggregate room and reservation data without turning GraphQL into a second write API.

## REST versus GraphQL

| Scenario | Recommended surface | Why |
| --- | --- | --- |
| Create or change reservations | REST `/api/v1` | Mutations stay explicit, versioned, and operationally governed through HTTP semantics and ProblemDetails |
| Query room lists with consumer-selected fields | GraphQL `/graphql` | Consumers can shape the payload and avoid over-fetching |
| Query reservations per location or consumer-specific slices | GraphQL `/graphql` | Filters, paging, and sorting can be combined in one read request |
| Query availability across multiple rooms in one call | GraphQL `/graphql` | The read model returns a single consumer-oriented overview instead of multiple REST round-trips |
| Health or operational probing | REST `/health*` | Operational contracts remain explicit and infrastructure-friendly |

## Query catalogue

The current GraphQL query surface contains:

- `rooms` - room discovery with search, location filter, minimum capacity, paging, and explicit sort order
- `room` - single room lookup
- `roomAvailability` - availability check for one room and one time window
- `reservations` - reservation read model with room, location, reserver, status, time-window filters, paging, and explicit sort order
- `reservation` - single reservation lookup
- `roomAvailabilityOverview` - multi-room availability overview for one requested time window
- `occupancyOverview` - occupancy summaries grouped by location for one requested time window

## Governance rules

- GraphQL stays **read-only**. REST remains the authoritative mutation surface.
- Query controls are **explicit**. Consumers only get the filters, sort orders, and paging options that the platform intentionally supports.
- `first` is capped at **50** items.
- GraphQL request governance applies execution cost limits:
  - `MaxFieldCost = 250`
  - `MaxTypeCost = 250`
- Location filters are exact-match filters. Use `search` on `rooms` when broader fuzzy matching is needed.
- Schema additions must ship with documentation and GraphQL contract tests.
- Field-level authorization is intentionally deferred to the Entra ID phase so the current read model can evolve without locking in the wrong identity model too early.

## Example queries

### Room discovery

```graphql
query {
  rooms(
    search: "Office"
    minimumCapacity: 4
    sortBy: CAPACITY_DESCENDING
    first: 10
  ) {
    code
    name
    location
    capacity
  }
}
```

### Availability overview for room selection

```graphql
query {
  roomAvailabilityOverview(
    fromUtc: "2026-04-11T09:00:00Z"
    toUtc: "2026-04-11T10:00:00Z"
    sortBy: AVAILABILITY_DESCENDING
    first: 10
  ) {
    roomCode
    roomName
    location
    capacity
    isAvailable
    conflictCount
  }
}
```

### Occupancy overview by location

```graphql
query {
  occupancyOverview(
    fromUtc: "2026-04-11T09:00:00Z"
    toUtc: "2026-04-11T10:00:00Z"
    sortBy: LOCATION_ASCENDING
    first: 10
  ) {
    location
    totalRooms
    reservedRooms
    availableRooms
    roomOccupancyRate
    capacityOccupancyRate
  }
}
```

## Contract expectations

- Additive query fields are allowed within the current major API line when they do not break existing consumers.
- Breaking GraphQL changes must follow the same deliberate governance path as REST changes.
- Consumer-specific read models are encouraged when they reduce client orchestration, but they must stay read-focused and not leak command behavior into GraphQL.
