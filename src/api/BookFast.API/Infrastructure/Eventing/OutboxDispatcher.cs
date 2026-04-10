// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxDispatcher.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookFast.API.Infrastructure.Eventing;

public sealed class OutboxDispatcher
{
    private const int MinimumProcessingLeaseSeconds = 30;

    private readonly BookFastDbContext _dbContext;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly EventingOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        BookFastDbContext dbContext,
        IIntegrationEventPublisher publisher,
        TimeProvider timeProvider,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        this._dbContext = dbContext;
        this._publisher = publisher;
        this._timeProvider = timeProvider;
        this._options = options.Value;
        this._logger = logger;
    }

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        int batchSize = Math.Max(1, this._options.BatchSize);
        DateTime nowUtc = this._timeProvider.GetUtcNow().UtcDateTime;
        Guid[] candidateIds = [..await this._dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message =>
                (message.Status == OutboxMessageStatus.Pending || message.Status == OutboxMessageStatus.Processing) &&
                message.NextAttemptUtc <= nowUtc)
            .OrderBy(message => message.OccurredUtc)
            .Select(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken)];

        int publishedCount = 0;
        foreach (Guid candidateId in candidateIds)
        {
            OutboxMessageEntity? message = await this.ClaimMessageAsync(candidateId, cancellationToken);
            if (message is null)
            {
                continue;
            }

            DateTime attemptUtc = message.LastAttemptUtc ?? this._timeProvider.GetUtcNow().UtcDateTime;

            try
            {
                await this._publisher.PublishAsync(ToEnvelope(message), cancellationToken);

                message.Status = OutboxMessageStatus.Published;
                message.PublishedUtc = this._timeProvider.GetUtcNow().UtcDateTime;
                message.LastError = null;
                publishedCount += 1;

                this._logger.LogInformation(
                    "Published outbox message {MessageId} with event type {EventType} on attempt {Attempt}.",
                    message.Id,
                    message.EventType,
                    message.DeliveryAttemptCount);
            }
            catch (Exception exception)
            {
                message.LastError = Truncate(exception.ToString(), 4000);

                if (message.DeliveryAttemptCount >= Math.Max(1, this._options.MaxPublishAttempts))
                {
                    message.Status = OutboxMessageStatus.DeadLettered;
                    this._logger.LogError(
                        exception,
                        "Outbox message {MessageId} for event type {EventType} moved to dead letter after {AttemptCount} attempts.",
                        message.Id,
                        message.EventType,
                        message.DeliveryAttemptCount);
                }
                else
                {
                    message.Status = OutboxMessageStatus.Pending;
                    message.NextAttemptUtc = attemptUtc.AddSeconds(Math.Max(1, this._options.PublishRetryDelaySeconds));
                    this._logger.LogWarning(
                        exception,
                        "Publishing outbox message {MessageId} for event type {EventType} failed on attempt {AttemptCount}.",
                        message.Id,
                        message.EventType,
                        message.DeliveryAttemptCount);
                }
            }

            await this._dbContext.SaveChangesAsync(cancellationToken);
        }

        return publishedCount;
    }

    private async Task<OutboxMessageEntity?> ClaimMessageAsync(Guid candidateId, CancellationToken cancellationToken)
    {
        DateTime attemptUtc = this._timeProvider.GetUtcNow().UtcDateTime;
        DateTime leaseExpiresUtc = attemptUtc.AddSeconds(Math.Max(MinimumProcessingLeaseSeconds, this._options.PublishRetryDelaySeconds));

        int updatedRows = await this._dbContext.OutboxMessages
            .Where(message => message.Id == candidateId)
            .Where(message =>
                (message.Status == OutboxMessageStatus.Pending || message.Status == OutboxMessageStatus.Processing) &&
                message.NextAttemptUtc <= attemptUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                    .SetProperty(message => message.DeliveryAttemptCount, message => message.DeliveryAttemptCount + 1)
                    .SetProperty(message => message.LastAttemptUtc, attemptUtc)
                    .SetProperty(message => message.NextAttemptUtc, leaseExpiresUtc),
                cancellationToken);

        if (updatedRows == 0)
        {
            return null;
        }

        return await this._dbContext.OutboxMessages.SingleAsync(message => message.Id == candidateId, cancellationToken);
    }

    private static OutboxMessageEnvelope ToEnvelope(OutboxMessageEntity message)
    {
        return new OutboxMessageEnvelope(
            message.Id,
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            new DateTimeOffset(DateTime.SpecifyKind(message.OccurredUtc, DateTimeKind.Utc)),
            message.PayloadJson,
            message.CorrelationId);
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
