// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : AvailabilityConflictResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Rooms;

public sealed record AvailabilityConflictResponse(
    Guid ReservationId,
    string ReservedBy,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status);
