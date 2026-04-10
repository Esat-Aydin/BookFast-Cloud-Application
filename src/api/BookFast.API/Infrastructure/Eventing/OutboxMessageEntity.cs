// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxMessageEntity.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public OutboxMessageStatus Status { get; set; }

    public int DeliveryAttemptCount { get; set; }

    public DateTime NextAttemptUtc { get; set; }

    public DateTime? LastAttemptUtc { get; set; }

    public DateTime? PublishedUtc { get; set; }

    public string? LastError { get; set; }
}
