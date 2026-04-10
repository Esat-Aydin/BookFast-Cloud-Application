// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingReservationSyncEntity.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public sealed class ReportingReservationSyncEntity
{
    public Guid ReservationId { get; set; }

    public Guid RoomId { get; set; }

    public string RoomCode { get; set; } = string.Empty;

    public string RoomName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string ReservedBy { get; set; } = string.Empty;

    public string? Purpose { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid SourceMessageId { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime LastSyncedUtc { get; set; }
}
