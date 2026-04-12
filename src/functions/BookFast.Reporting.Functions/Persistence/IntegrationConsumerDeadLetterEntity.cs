// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IntegrationConsumerDeadLetterEntity.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

namespace BookFast.Reporting.Functions.Persistence;

public sealed class IntegrationConsumerDeadLetterEntity
{
    public Guid Id { get; set; }

    public string ConsumerName { get; set; } = string.Empty;

    public Guid MessageId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int DeliveryAttemptCount { get; set; }

    public DateTime FailedUtc { get; set; }

    public string? CorrelationId { get; set; }
}
