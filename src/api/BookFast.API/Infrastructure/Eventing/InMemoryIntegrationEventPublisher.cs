// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : InMemoryIntegrationEventPublisher.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookFast.API.Infrastructure.Eventing;

public sealed class InMemoryIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly EventingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryIntegrationEventPublisher> _logger;

    public InMemoryIntegrationEventPublisher(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<EventingOptions> options,
        TimeProvider timeProvider,
        ILogger<InMemoryIntegrationEventPublisher> logger)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._options = options.Value;
        this._timeProvider = timeProvider;
        this._logger = logger;
    }

    public async Task PublishAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope discoveryScope = this._serviceScopeFactory.CreateAsyncScope();
        string[] consumerNames = [..discoveryScope.ServiceProvider
            .GetServices<IIntegrationEventConsumer>()
            .Where(consumer => consumer.CanHandle(message.EventType))
            .Select(consumer => consumer.ConsumerName)
            .Distinct(StringComparer.Ordinal)];

        if (consumerNames.Length == 0)
        {
            this._logger.LogInformation(
                "No local integration consumer is registered for event type {EventType}.",
                message.EventType);
            return;
        }

        foreach (string consumerName in consumerNames)
        {
            await this.DeliverToConsumerAsync(message, consumerName, cancellationToken);
        }
    }

    private async Task DeliverToConsumerAsync(
        OutboxMessageEnvelope message,
        string consumerName,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Max(1, this._options.LocalConsumerMaxAttempts);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using AsyncServiceScope scope = this._serviceScopeFactory.CreateAsyncScope();
            BookFastDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookFastDbContext>();

            bool alreadyProcessed = await dbContext.IntegrationConsumerStates
                .AsNoTracking()
                .AnyAsync(
                    state => state.ConsumerName == consumerName && state.MessageId == message.MessageId,
                    cancellationToken);

            if (alreadyProcessed)
            {
                this._logger.LogInformation(
                    "Skipping duplicate delivery of message {MessageId} for consumer {ConsumerName}.",
                    message.MessageId,
                    consumerName);
                return;
            }

            IIntegrationEventConsumer consumer = scope.ServiceProvider
                .GetServices<IIntegrationEventConsumer>()
                .Single(candidate => candidate.ConsumerName == consumerName);

            try
            {
                await consumer.HandleAsync(message, cancellationToken);

                dbContext.IntegrationConsumerStates.Add(new IntegrationConsumerStateEntity
                {
                    ConsumerName = consumerName,
                    MessageId = message.MessageId,
                    EventType = message.EventType,
                    CorrelationId = message.CorrelationId,
                    ProcessedUtc = this._timeProvider.GetUtcNow().UtcDateTime
                });

                await dbContext.SaveChangesAsync(cancellationToken);

                this._logger.LogInformation(
                    "Consumer {ConsumerName} processed message {MessageId}.",
                    consumerName,
                    message.MessageId);

                return;
            }
            catch (DbUpdateException exception) when (IsDuplicateConsumerStateViolation(exception))
            {
                this._logger.LogInformation(
                    "Skipping duplicate delivery of message {MessageId} for consumer {ConsumerName} after concurrent processing.",
                    message.MessageId,
                    consumerName);

                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                this._logger.LogWarning(
                    exception,
                    "Consumer {ConsumerName} failed processing message {MessageId} on attempt {Attempt}.",
                    consumerName,
                    message.MessageId,
                    attempt);

                if (this._options.LocalConsumerRetryDelayMilliseconds > 0)
                {
                    await Task.Delay(this._options.LocalConsumerRetryDelayMilliseconds, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                IntegrationConsumerDeadLetterEntity? existingDeadLetter = await dbContext.IntegrationConsumerDeadLetters
                    .SingleOrDefaultAsync(
                        deadLetter => deadLetter.ConsumerName == consumerName && deadLetter.MessageId == message.MessageId,
                        cancellationToken);

                if (existingDeadLetter is null)
                {
                    dbContext.IntegrationConsumerDeadLetters.Add(new IntegrationConsumerDeadLetterEntity
                    {
                        Id = Guid.NewGuid(),
                        ConsumerName = consumerName,
                        MessageId = message.MessageId,
                        EventType = message.EventType,
                        PayloadJson = message.PayloadJson,
                        CorrelationId = message.CorrelationId,
                        DeliveryAttemptCount = attempt,
                        FailedUtc = this._timeProvider.GetUtcNow().UtcDateTime,
                        Reason = Truncate(exception.ToString(), 4000)
                    });
                }
                else
                {
                    existingDeadLetter.DeliveryAttemptCount = attempt;
                    existingDeadLetter.FailedUtc = this._timeProvider.GetUtcNow().UtcDateTime;
                    existingDeadLetter.Reason = Truncate(exception.ToString(), 4000);
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                this._logger.LogError(
                    exception,
                    "Consumer {ConsumerName} dead-lettered message {MessageId} after {AttemptCount} attempts.",
                    consumerName,
                    message.MessageId,
                    attempt);

                return;
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static bool IsDuplicateConsumerStateViolation(DbUpdateException exception)
    {
        string message = exception.InnerException?.Message ?? exception.Message;

        return message.Contains("IntegrationConsumerStates", StringComparison.OrdinalIgnoreCase) &&
               (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
    }
}
