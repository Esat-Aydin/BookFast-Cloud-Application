// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Reservation.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Domain;

public sealed record Reservation(
    Guid Id,
    Guid RoomId,
    string ReservedBy,
    string? Purpose,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset CreatedUtc,
    ReservationStatus Status);
