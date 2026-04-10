// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomAvailabilityChangedIntegrationEvent.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Integration;

public sealed record RoomAvailabilityChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredUtc,
    Guid RoomId,
    Guid ReservationId,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string Reason,
    string? CorrelationId);
