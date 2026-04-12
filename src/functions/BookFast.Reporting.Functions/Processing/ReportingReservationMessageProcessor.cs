// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingReservationMessageProcessor.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

using BookFast.Integration.Contracts;
using BookFast.Reporting.Functions.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookFast.Reporting.Functions.Processing;

public sealed class ReportingReservationMessageProcessor
{
    public const string ConsumerName = "ReportingReservationSync";

    private readonly ReportingDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportingReservationMessageProcessor> _logger;

    public ReportingReservationMessageProcessor(
        ReportingDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<ReportingReservationMessageProcessor> logger)
    {
        this._dbContext = dbContext;
        this._timeProvider = timeProvider;
        this._logger = logger;
    }

    public async Task<MessageProcessingOutcome> ProcessAsync(
        Guid messageId,
        string subject,
        string payloadJson,
        string? correlationId,
        int deliveryCount,
        CancellationToken cancellationToken)
    {
        bool alreadyProcessed = await this._dbContext.IntegrationConsumerStates
            .AsNoTracking()
            .AnyAsync(
                state => state.ConsumerName == ConsumerName && state.MessageId == messageId,
                cancellationToken);

        if (alreadyProcessed)
        {
            this._logger.LogInformation(
                "Skipping duplicate delivery of message {MessageId} for consumer {ConsumerName}.",
                messageId,
                ConsumerName);

            return MessageProcessingOutcome.AlreadyProcessed;
        }

        if (subject != IntegrationEventNames.ReservationCreated)
        {
            this._logger.LogInformation(
                "Consumer {ConsumerName} has no handler for subject {Subject}, message {MessageId} will be skipped.",
                ConsumerName,
                subject,
                messageId);

            return MessageProcessingOutcome.Skipped;
        }

        ReservationCreatedIntegrationEvent integrationEvent =
            IntegrationEventJsonSerializer.Deserialize<ReservationCreatedIntegrationEvent>(payloadJson);

        RoomEntity? room = await this._dbContext.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == integrationEvent.RoomId, cancellationToken);

        if (room is null)
        {
            this._logger.LogError(
                "Consumer {ConsumerName} could not resolve room '{RoomId}' for reservation '{ReservationId}'. Message {MessageId} will be dead-lettered.",
                ConsumerName,
                integrationEvent.RoomId,
                integrationEvent.ReservationId,
                messageId);

            await this.RecordDeadLetterAsync(
                messageId,
                subject,
                payloadJson,
                correlationId,
                deliveryCount,
                $"Room '{integrationEvent.RoomId}' not found for reservation '{integrationEvent.ReservationId}'.",
                cancellationToken);

            return MessageProcessingOutcome.DeadLettered;
        }

        ReportingReservationSyncEntity? existingSync = await this._dbContext.ReportingReservationSyncs
            .SingleOrDefaultAsync(sync => sync.ReservationId == integrationEvent.ReservationId, cancellationToken);

        if (existingSync is null)
        {
            existingSync = new ReportingReservationSyncEntity
            {
                ReservationId = integrationEvent.ReservationId
            };

            await this._dbContext.ReportingReservationSyncs.AddAsync(existingSync, cancellationToken);
        }

        existingSync.RoomId = integrationEvent.RoomId;
        existingSync.RoomCode = room.Code;
        existingSync.RoomName = room.Name;
        existingSync.Location = room.Location;
        existingSync.ReservedBy = integrationEvent.ReservedBy;
        existingSync.Purpose = integrationEvent.Purpose;
        existingSync.StartUtc = integrationEvent.StartUtc.UtcDateTime;
        existingSync.EndUtc = integrationEvent.EndUtc.UtcDateTime;
        existingSync.CreatedUtc = integrationEvent.CreatedUtc.UtcDateTime;
        existingSync.Status = integrationEvent.Status;
        existingSync.SourceMessageId = messageId;
        existingSync.CorrelationId = correlationId;
        existingSync.LastSyncedUtc = this._timeProvider.GetUtcNow().UtcDateTime;

        this._dbContext.IntegrationConsumerStates.Add(new IntegrationConsumerStateEntity
        {
            ConsumerName = ConsumerName,
            MessageId = messageId,
            EventType = subject,
            CorrelationId = correlationId,
            ProcessedUtc = this._timeProvider.GetUtcNow().UtcDateTime
        });

        try
        {
            await this._dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateProcessingViolation(exception))
        {
            this._logger.LogInformation(
                "Skipping duplicate delivery of message {MessageId} for consumer {ConsumerName} after concurrent processing.",
                messageId,
                ConsumerName);

            return MessageProcessingOutcome.AlreadyProcessed;
        }

        this._logger.LogInformation(
            "Consumer {ConsumerName} processed message {MessageId} for reservation {ReservationId}.",
            ConsumerName,
            messageId,
            integrationEvent.ReservationId);

        return MessageProcessingOutcome.Processed;
    }

    private async Task RecordDeadLetterAsync(
        Guid messageId,
        string subject,
        string payloadJson,
        string? correlationId,
        int deliveryCount,
        string reason,
        CancellationToken cancellationToken)
    {
        IntegrationConsumerDeadLetterEntity? existingDeadLetter = await this._dbContext.IntegrationConsumerDeadLetters
            .SingleOrDefaultAsync(
                deadLetter => deadLetter.ConsumerName == ConsumerName && deadLetter.MessageId == messageId,
                cancellationToken);

        if (existingDeadLetter is null)
        {
            this._dbContext.IntegrationConsumerDeadLetters.Add(new IntegrationConsumerDeadLetterEntity
            {
                Id = Guid.NewGuid(),
                ConsumerName = ConsumerName,
                MessageId = messageId,
                EventType = subject,
                PayloadJson = payloadJson,
                CorrelationId = correlationId,
                DeliveryAttemptCount = deliveryCount,
                FailedUtc = this._timeProvider.GetUtcNow().UtcDateTime,
                Reason = Truncate(reason, 4000)
            });
        }
        else
        {
            existingDeadLetter.DeliveryAttemptCount = deliveryCount;
            existingDeadLetter.FailedUtc = this._timeProvider.GetUtcNow().UtcDateTime;
            existingDeadLetter.Reason = Truncate(reason, 4000);
        }

        try
        {
            await this._dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateDeadLetterViolation(exception))
        {
            this._logger.LogInformation(
                "Dead-letter record for consumer {ConsumerName} and message {MessageId} already exists due to concurrent processing.",
                ConsumerName,
                messageId);
        }

        this._logger.LogError(
            "Consumer {ConsumerName} dead-lettered message {MessageId} after {DeliveryCount} delivery attempts. Reason: {Reason}",
            ConsumerName,
            messageId,
            deliveryCount,
            reason);
    }

    private static bool IsDuplicateProcessingViolation(DbUpdateException exception)
    {
        return IsDuplicateConstraintViolation(exception, "IntegrationConsumerStates") ||
               IsDuplicateConstraintViolation(exception, "ReportingReservationSyncs");
    }

    private static bool IsDuplicateDeadLetterViolation(DbUpdateException exception)
    {
        return IsDuplicateConstraintViolation(exception, "IntegrationConsumerDeadLetters");
    }

    private static bool IsDuplicateConstraintViolation(DbUpdateException exception, string tableName)
    {
        string message = exception.InnerException?.Message ?? exception.Message;

        return message.Contains(tableName, StringComparison.OrdinalIgnoreCase) &&
               (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
