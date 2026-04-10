// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationEntity.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

namespace BookFast.API.Infrastructure.Persistence;

public sealed class ReservationEntity
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public string ReservedBy { get; set; } = string.Empty;

    public string? Purpose { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ReservationStatus Status { get; set; }

    public RoomEntity? Room { get; set; }
}
