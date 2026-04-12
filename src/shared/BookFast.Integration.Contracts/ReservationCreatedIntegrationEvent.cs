// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationCreatedIntegrationEvent.cs
//  Project         : BookFast.Integration.Contracts
// ******************************************************************************

namespace BookFast.Integration.Contracts;

public sealed record ReservationCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredUtc,
    Guid ReservationId,
    Guid RoomId,
    string ReservedBy,
    string? Purpose,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset CreatedUtc,
    string Status,
    string? CorrelationId);
