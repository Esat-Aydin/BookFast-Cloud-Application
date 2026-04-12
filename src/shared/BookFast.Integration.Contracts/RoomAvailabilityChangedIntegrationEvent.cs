// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomAvailabilityChangedIntegrationEvent.cs
//  Project         : BookFast.Integration.Contracts
// ******************************************************************************

namespace BookFast.Integration.Contracts;

public sealed record RoomAvailabilityChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredUtc,
    Guid RoomId,
    Guid ReservationId,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset EffectiveToUtc,
    string Reason,
    string? CorrelationId);
