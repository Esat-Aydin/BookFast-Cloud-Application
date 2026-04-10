// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IntegrationConsumerStateEntity.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public sealed class IntegrationConsumerStateEntity
{
    public string ConsumerName { get; set; } = string.Empty;

    public Guid MessageId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public DateTime ProcessedUtc { get; set; }

    public string? CorrelationId { get; set; }
}
