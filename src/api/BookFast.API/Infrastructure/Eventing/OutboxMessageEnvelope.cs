// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxMessageEnvelope.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public sealed record OutboxMessageEnvelope(
    Guid MessageId,
    string EventType,
    string AggregateType,
    Guid AggregateId,
    DateTimeOffset OccurredUtc,
    string PayloadJson,
    string? CorrelationId);
