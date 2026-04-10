// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ServiceBusIntegrationEventPublisher.cs
//  Project         : BookFast.API
// ******************************************************************************

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Options;

namespace BookFast.API.Infrastructure.Eventing;

public sealed class ServiceBusIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly EventingOptions _options;
    private readonly ILogger<ServiceBusIntegrationEventPublisher> _logger;

    public ServiceBusIntegrationEventPublisher(
        ServiceBusClient serviceBusClient,
        IOptions<EventingOptions> options,
        ILogger<ServiceBusIntegrationEventPublisher> logger)
    {
        this._serviceBusClient = serviceBusClient;
        this._options = options.Value;
        this._logger = logger;
    }

    public async Task PublishAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken)
    {
        await using ServiceBusSender sender = this._serviceBusClient.CreateSender(this._options.ServiceBusTopicName);

        ServiceBusMessage serviceBusMessage = new(message.PayloadJson)
        {
            MessageId = message.MessageId.ToString(),
            Subject = message.EventType,
            CorrelationId = message.CorrelationId
        };

        serviceBusMessage.ApplicationProperties["aggregateType"] = message.AggregateType;
        serviceBusMessage.ApplicationProperties["aggregateId"] = message.AggregateId.ToString();
        serviceBusMessage.ApplicationProperties["occurredUtc"] = message.OccurredUtc.ToString("O");

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

        this._logger.LogInformation(
            "Published outbox message {MessageId} to Service Bus topic {TopicName}.",
            message.MessageId,
            this._options.ServiceBusTopicName);
    }
}
