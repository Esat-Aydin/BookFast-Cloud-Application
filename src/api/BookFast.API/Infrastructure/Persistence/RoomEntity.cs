// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomEntity.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Persistence;

public sealed class RoomEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string AmenitiesJson { get; set; } = "[]";

    public ICollection<ReservationEntity> Reservations { get; set; } = [];
}
