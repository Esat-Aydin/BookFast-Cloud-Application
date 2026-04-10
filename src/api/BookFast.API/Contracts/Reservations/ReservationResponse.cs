// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid RoomId,
    string RoomCode,
    string RoomName,
    string ReservedBy,
    string? Purpose,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset CreatedUtc,
    string Status);
