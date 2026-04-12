// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingReservationFunction.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

using System.Security.Cryptography;
using System.Text;

using Azure.Messaging.ServiceBus;

using BookFast.Reporting.Functions.Processing;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BookFast.Reporting.Functions;

public sealed class ReportingReservationFunction
{
    private readonly ReportingReservationMessageProcessor _processor;
    private readonly ILogger<ReportingReservationFunction> _logger;

    public ReportingReservationFunction(
        ReportingReservationMessageProcessor processor,
        ILogger<ReportingReservationFunction> logger)
    {
        this._processor = processor;
        this._logger = logger;
    }

    [Function(nameof(ReportingReservationFunction))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            topicName: "%ServiceBus__TopicName%",
            subscriptionName: "%ServiceBus__SubscriptionName%",
            Connection = "BookFastServiceBus")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
    {
        this._logger.LogInformation(
            "Processing Service Bus message {MessageId} with subject {Subject}.",
            message.MessageId,
            message.Subject);

        string rawMessageId = message.MessageId ?? string.Empty;
        Guid messageId = ResolveMessageId(rawMessageId, out bool usedFallback);

        if (usedFallback)
        {
            this._logger.LogWarning(
                "Service Bus message id '{RawMessageId}' is not a GUID; using deterministic hash {ResolvedMessageId} for idempotency.",
                rawMessageId,
                messageId);
        }

        MessageProcessingOutcome outcome = await this._processor.ProcessAsync(
            messageId,
            message.Subject ?? string.Empty,
            message.Body.ToString(),
            message.CorrelationId,
            message.DeliveryCount,
            cancellationToken);

        switch (outcome)
        {
            case MessageProcessingOutcome.Processed:
                this._logger.LogInformation(
                    "Message {MessageId} processed successfully.",
                    message.MessageId);
                await messageActions.CompleteMessageAsync(message, cancellationToken);
                break;

            case MessageProcessingOutcome.AlreadyProcessed:
                this._logger.LogInformation(
                    "Message {MessageId} was already processed; completing.",
                    message.MessageId);
                await messageActions.CompleteMessageAsync(message, cancellationToken);
                break;

            case MessageProcessingOutcome.Skipped:
                this._logger.LogInformation(
                    "Message {MessageId} has no matching handler for subject {Subject}; completing.",
                    message.MessageId,
                    message.Subject);
                await messageActions.CompleteMessageAsync(message, cancellationToken);
                break;

            case MessageProcessingOutcome.DeadLettered:
                this._logger.LogWarning(
                    "Message {MessageId} dead-lettered to local table; completing Service Bus message.",
                    message.MessageId);
                await messageActions.CompleteMessageAsync(message, cancellationToken);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unexpected processing outcome '{outcome}' for message {message.MessageId}.");
        }
    }

    private static Guid ResolveMessageId(string messageId, out bool usedFallback)
    {
        if (Guid.TryParse(messageId, out Guid parsedMessageId))
        {
            usedFallback = false;
            return parsedMessageId;
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(messageId));
        usedFallback = true;

        return new Guid(hashBytes.AsSpan(0, 16));
    }
}
