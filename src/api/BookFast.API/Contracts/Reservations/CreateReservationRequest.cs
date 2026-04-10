// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : CreateReservationRequest.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Reservations;

public sealed record CreateReservationRequest
{
    public Guid RoomId { get; init; }

    public required string ReservedBy { get; init; }

    public string? Purpose { get; init; }

    public DateTimeOffset StartUtc { get; init; }

    public DateTimeOffset EndUtc { get; init; }
}
