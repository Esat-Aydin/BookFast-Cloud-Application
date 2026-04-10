// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : EventingOptions.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public sealed class EventingOptions
{
    public const string SectionName = "Eventing";

    public IntegrationTransportMode Mode { get; set; } = IntegrationTransportMode.InMemory;

    public bool EnableBackgroundDispatcher { get; set; } = true;

    public int BatchSize { get; set; } = 20;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int MaxPublishAttempts { get; set; } = 5;

    public int PublishRetryDelaySeconds { get; set; } = 15;

    public int LocalConsumerMaxAttempts { get; set; } = 3;

    public int LocalConsumerRetryDelayMilliseconds { get; set; } = 100;

    public string ServiceBusTopicName { get; set; } = "bookfast.integration";
}
