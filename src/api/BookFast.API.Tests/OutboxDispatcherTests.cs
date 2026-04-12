// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxDispatcherTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Domain;
using BookFast.API.Infrastructure.Eventing;
using BookFast.API.Infrastructure.Persistence;
using BookFast.API.Services;

using BookFast.Integration.Contracts;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookFast.API.Tests;

public sealed class OutboxDispatcherTests
{
    [Fact]
    public async Task DispatchPendingAsync_ShouldPublishMessagesAndCreateReportingProjection()
    {
        await using EventingTestHarness harness = await EventingTestHarness.CreateAsync();

        Reservation reservation = await harness.CreateReservationAsync(
            reservedBy: "Phase 3 Publisher",
            correlationId: "corr-phase3-dispatch");

        int publishedCount = await harness.DispatchPendingAsync();

        OutboxMessageEntity[] outboxMessages = await harness.QueryAsync(
            dbContext => dbContext.OutboxMessages.OrderBy(message => message.EventType));
        ReportingReservationSyncEntity[] reportingSyncs = await harness.QueryAsync(
            dbContext => dbContext.ReportingReservationSyncs.OrderBy(sync => sync.ReservationId));
        IntegrationConsumerStateEntity[] consumerStates = await harness.QueryAsync(
            dbContext => dbContext.IntegrationConsumerStates.OrderBy(state => state.ConsumerName));

        Assert.Equal(2, publishedCount);
        Assert.Equal(2, outboxMessages.Length);
        Assert.All(outboxMessages, message => Assert.Equal(OutboxMessageStatus.Published, message.Status));
        Assert.Single(reportingSyncs);
        Assert.Equal(reservation.Id, reportingSyncs[0].ReservationId);
        Assert.Equal("Phase 3 Publisher", reportingSyncs[0].ReservedBy);
        Assert.Equal("corr-phase3-dispatch", reportingSyncs[0].CorrelationId);
        Assert.Single(consumerStates);
        Assert.Equal("ReportingReservationSync", consumerStates[0].ConsumerName);
    }

    [Fact]
    public async Task DispatchPendingAsync_ShouldRemainIdempotent_WhenOutboxMessageIsPublishedTwice()
    {
        await using EventingTestHarness harness = await EventingTestHarness.CreateAsync();

        await harness.CreateReservationAsync(
            reservedBy: "Phase 3 Duplicate",
            correlationId: "corr-phase3-duplicate");

        await harness.DispatchPendingAsync();
        await harness.ResetReservationCreatedOutboxMessageAsync();

        int publishedCount = await harness.DispatchPendingAsync();

        ReportingReservationSyncEntity[] reportingSyncs = await harness.QueryAsync(
            dbContext => dbContext.ReportingReservationSyncs.OrderBy(sync => sync.ReservationId));
        IntegrationConsumerStateEntity[] consumerStates = await harness.QueryAsync(
            dbContext => dbContext.IntegrationConsumerStates.OrderBy(state => state.ConsumerName));

        Assert.Equal(1, publishedCount);
        Assert.Single(reportingSyncs);
        Assert.Single(consumerStates);
    }

    [Fact]
    public async Task DispatchPendingAsync_ShouldDeadLetterPoisonConsumerMessage()
    {
        await using EventingTestHarness harness = await EventingTestHarness.CreateAsync(includeThrowingConsumer: true);

        await harness.CreateReservationAsync(
            reservedBy: "Phase 3 Poison",
            correlationId: "corr-phase3-deadletter");

        await harness.DispatchPendingAsync();

        IntegrationConsumerDeadLetterEntity[] deadLetters = await harness.QueryAsync(
            dbContext => dbContext.IntegrationConsumerDeadLetters.OrderBy(deadLetter => deadLetter.ConsumerName));
        ReportingReservationSyncEntity[] reportingSyncs = await harness.QueryAsync(
            dbContext => dbContext.ReportingReservationSyncs.OrderBy(sync => sync.ReservationId));
        OutboxMessageEntity[] outboxMessages = await harness.QueryAsync(
            dbContext => dbContext.OutboxMessages.OrderBy(message => message.EventType));

        Assert.Single(deadLetters);
        Assert.Equal("ThrowingReservationCreatedConsumer", deadLetters[0].ConsumerName);
        Assert.Equal(2, deadLetters[0].DeliveryAttemptCount);
        Assert.Single(reportingSyncs);
        Assert.All(outboxMessages, message => Assert.Equal(OutboxMessageStatus.Published, message.Status));
    }

    private sealed class EventingTestHarness : IAsyncDisposable
    {
        private static readonly Guid AmsterdamBoardRoomId = Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901");

        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;

        private EventingTestHarness(
            SqliteConnection connection,
            ServiceProvider serviceProvider,
            FixedTimeProvider timeProvider)
        {
            this._connection = connection;
            this._serviceProvider = serviceProvider;
            this.TimeProvider = timeProvider;
        }

        public FixedTimeProvider TimeProvider { get; }

        public static async Task<EventingTestHarness> CreateAsync(bool includeThrowingConsumer = false)
        {
            SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            ServiceCollection services = new();
            services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Warning));

            FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddDbContext<BookFastDbContext>(options => options.UseSqlite(connection));
            services.AddScoped<IBookFastCatalog, SqlBookFastCatalog>();
            services.AddScoped<OutboxDispatcher>();
            services.AddScoped<IIntegrationEventPublisher, InMemoryIntegrationEventPublisher>();
            services.AddScoped<IIntegrationEventConsumer, ReportingReservationIntegrationConsumer>();

            if (includeThrowingConsumer)
            {
                services.AddScoped<IIntegrationEventConsumer, ThrowingReservationCreatedConsumer>();
            }

            services.AddOptions<EventingOptions>()
                .Configure(options =>
                {
                    options.Mode = IntegrationTransportMode.InMemory;
                    options.EnableBackgroundDispatcher = false;
                    options.BatchSize = 20;
                    options.MaxPublishAttempts = 3;
                    options.PublishRetryDelaySeconds = 1;
                    options.LocalConsumerMaxAttempts = 2;
                    options.LocalConsumerRetryDelayMilliseconds = 1;
                    options.ServiceBusTopicName = "bookfast.integration";
                });

            ServiceProvider serviceProvider = services.BuildServiceProvider(validateScopes: true);

            using IServiceScope scope = serviceProvider.CreateScope();
            BookFastDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookFastDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            return new EventingTestHarness(connection, serviceProvider, timeProvider);
        }

        public async Task<Reservation> CreateReservationAsync(string reservedBy, string correlationId)
        {
            await using AsyncServiceScope scope = this._serviceProvider.CreateAsyncScope();
            IBookFastCatalog catalog = scope.ServiceProvider.GetRequiredService<IBookFastCatalog>();
            Room room = (await catalog.ListRoomsAsync(CancellationToken.None))
                .Single(candidate => candidate.Id == AmsterdamBoardRoomId);
            DateTimeOffset startUtc = this.TimeProvider.GetUtcNow().AddHours(2);
            DateTimeOffset endUtc = startUtc.AddHours(1);

            ReservationCreationResult result = await catalog.CreateReservationAsync(
                room.Id,
                reservedBy,
                "Phase 3 reservation",
                startUtc,
                endUtc,
                correlationId,
                CancellationToken.None);

            return result.Reservation ?? throw new InvalidOperationException("Reservation creation did not produce a reservation.");
        }

        public async Task<int> DispatchPendingAsync()
        {
            await using AsyncServiceScope scope = this._serviceProvider.CreateAsyncScope();
            OutboxDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

            return await dispatcher.DispatchPendingAsync(CancellationToken.None);
        }

        public async Task ResetReservationCreatedOutboxMessageAsync()
        {
            await using AsyncServiceScope scope = this._serviceProvider.CreateAsyncScope();
            BookFastDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookFastDbContext>();
            OutboxMessageEntity message = await dbContext.OutboxMessages
                .SingleAsync(candidate => candidate.EventType == IntegrationEventNames.ReservationCreated);

            message.Status = OutboxMessageStatus.Pending;
            message.PublishedUtc = null;
            message.NextAttemptUtc = this.TimeProvider.GetUtcNow().UtcDateTime;
            message.LastError = null;

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<T[]> QueryAsync<T>(Func<BookFastDbContext, IQueryable<T>> query)
        {
            await using AsyncServiceScope scope = this._serviceProvider.CreateAsyncScope();
            BookFastDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookFastDbContext>();

            return await query(dbContext).ToArrayAsync(CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await this._serviceProvider.DisposeAsync();
            await this._connection.DisposeAsync();
        }
    }

    private sealed class ThrowingReservationCreatedConsumer : IIntegrationEventConsumer
    {
        public string ConsumerName => "ThrowingReservationCreatedConsumer";

        public bool CanHandle(string eventType)
        {
            return eventType == IntegrationEventNames.ReservationCreated;
        }

        public Task HandleAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken)
        {
            ReservationCreatedIntegrationEvent integrationEvent =
                IntegrationEventJsonSerializer.Deserialize<ReservationCreatedIntegrationEvent>(message.PayloadJson);

            throw new InvalidOperationException(
                $"Simulated poison consumer failure for reservation '{integrationEvent.ReservationId}'.");
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this._utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this._utcNow;
        }
    }
}
