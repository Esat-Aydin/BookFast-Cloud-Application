// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingReservationMessageProcessorTests.cs
//  Project         : BookFast.Reporting.Functions.Tests
// ******************************************************************************

using BookFast.Integration.Contracts;
using BookFast.Reporting.Functions.Persistence;
using BookFast.Reporting.Functions.Processing;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookFast.Reporting.Functions.Tests;

public sealed class ReportingReservationMessageProcessorTests
{
    private static readonly Guid TestRoomId = Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901");
    private static readonly Guid TestReservationId = Guid.NewGuid();

    [Fact]
    public async Task ProcessAsync_ShouldUpsertReportingSync_WhenNewMessage()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid messageId = Guid.NewGuid();
        string payloadJson = BuildReservationCreatedPayload(messageId, TestRoomId, TestReservationId);

        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            messageId,
            IntegrationEventNames.ReservationCreated,
            payloadJson,
            correlationId: "corr-phase4-new",
            deliveryCount: 1);

        ReportingReservationSyncEntity[] syncs = await harness.QueryAsync(
            db => db.ReportingReservationSyncs.OrderBy(s => s.ReservationId));

        IntegrationConsumerStateEntity[] states = await harness.QueryAsync(
            db => db.IntegrationConsumerStates.OrderBy(s => s.ConsumerName));

        Assert.Equal(MessageProcessingOutcome.Processed, outcome);
        Assert.Single(syncs);
        Assert.Equal(TestReservationId, syncs[0].ReservationId);
        Assert.Equal("AMS-BOARD-01", syncs[0].RoomCode);
        Assert.Equal("Phase 4 Reservee", syncs[0].ReservedBy);
        Assert.Equal("corr-phase4-new", syncs[0].CorrelationId);
        Assert.Single(states);
        Assert.Equal(ReportingReservationMessageProcessor.ConsumerName, states[0].ConsumerName);
        Assert.Equal(messageId, states[0].MessageId);
    }

    [Fact]
    public async Task ProcessAsync_ShouldUpdateExistingSync_WhenSameReservationReprocessed()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid firstMessageId = Guid.NewGuid();
        string firstPayload = BuildReservationCreatedPayload(firstMessageId, TestRoomId, TestReservationId, reservedBy: "First Reservee");
        await harness.ProcessAsync(firstMessageId, IntegrationEventNames.ReservationCreated, firstPayload, "corr-1", 1);

        Guid secondMessageId = Guid.NewGuid();
        string secondPayload = BuildReservationCreatedPayload(secondMessageId, TestRoomId, TestReservationId, reservedBy: "Updated Reservee");
        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            secondMessageId,
            IntegrationEventNames.ReservationCreated,
            secondPayload,
            "corr-2",
            1);

        ReportingReservationSyncEntity[] syncs = await harness.QueryAsync(
            db => db.ReportingReservationSyncs.OrderBy(s => s.ReservationId));

        Assert.Equal(MessageProcessingOutcome.Processed, outcome);
        Assert.Single(syncs);
        Assert.Equal("Updated Reservee", syncs[0].ReservedBy);
        Assert.Equal(secondMessageId, syncs[0].SourceMessageId);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnAlreadyProcessed_WhenMessageIdAlreadyInConsumerState()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid messageId = Guid.NewGuid();
        string payloadJson = BuildReservationCreatedPayload(messageId, TestRoomId, TestReservationId);

        await harness.ProcessAsync(messageId, IntegrationEventNames.ReservationCreated, payloadJson, "corr-dup", 1);
        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            messageId,
            IntegrationEventNames.ReservationCreated,
            payloadJson,
            "corr-dup",
            2);

        ReportingReservationSyncEntity[] syncs = await harness.QueryAsync(
            db => db.ReportingReservationSyncs.OrderBy(s => s.ReservationId));

        IntegrationConsumerStateEntity[] states = await harness.QueryAsync(
            db => db.IntegrationConsumerStates.OrderBy(s => s.ConsumerName));

        Assert.Equal(MessageProcessingOutcome.AlreadyProcessed, outcome);
        Assert.Single(syncs);
        Assert.Single(states);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnAlreadyProcessed_WhenSubjectIsUnknown()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid messageId = Guid.NewGuid();

        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            messageId,
            "room.availability.changed.v1",
            "{}",
            correlationId: null,
            deliveryCount: 1);

        IntegrationConsumerStateEntity[] states = await harness.QueryAsync(
            db => db.IntegrationConsumerStates.OrderBy(s => s.ConsumerName));

        Assert.Equal(MessageProcessingOutcome.Skipped, outcome);
        Assert.Empty(states);
    }

    [Fact]
    public async Task ProcessAsync_ShouldDeadLetter_WhenRoomIsNotFound()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid unknownRoomId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        string payloadJson = BuildReservationCreatedPayload(messageId, unknownRoomId, TestReservationId);

        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            messageId,
            IntegrationEventNames.ReservationCreated,
            payloadJson,
            correlationId: "corr-no-room",
            deliveryCount: 3);

        IntegrationConsumerDeadLetterEntity[] deadLetters = await harness.QueryAsync(
            db => db.IntegrationConsumerDeadLetters.OrderBy(dl => dl.MessageId));

        ReportingReservationSyncEntity[] syncs = await harness.QueryAsync(
            db => db.ReportingReservationSyncs.OrderBy(s => s.ReservationId));

        Assert.Equal(MessageProcessingOutcome.DeadLettered, outcome);
        Assert.Single(deadLetters);
        Assert.Equal(ReportingReservationMessageProcessor.ConsumerName, deadLetters[0].ConsumerName);
        Assert.Equal(messageId, deadLetters[0].MessageId);
        Assert.Equal(3, deadLetters[0].DeliveryAttemptCount);
        Assert.Contains(unknownRoomId.ToString(), deadLetters[0].Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(syncs);
    }

    [Fact]
    public async Task ProcessAsync_ShouldUpdateDeadLetterRecord_WhenSameMessageDeadLetteredAgain()
    {
        await using ProcessorTestHarness harness = await ProcessorTestHarness.CreateAsync();

        Guid unknownRoomId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        string payloadJson = BuildReservationCreatedPayload(messageId, unknownRoomId, TestReservationId);

        await harness.ProcessAsync(messageId, IntegrationEventNames.ReservationCreated, payloadJson, null, 1);
        MessageProcessingOutcome outcome = await harness.ProcessAsync(
            messageId,
            IntegrationEventNames.ReservationCreated,
            payloadJson,
            null,
            2);

        IntegrationConsumerDeadLetterEntity[] deadLetters = await harness.QueryAsync(
            db => db.IntegrationConsumerDeadLetters.OrderBy(dl => dl.MessageId));

        Assert.Equal(MessageProcessingOutcome.DeadLettered, outcome);
        Assert.Single(deadLetters);
        Assert.Equal(2, deadLetters[0].DeliveryAttemptCount);
    }

    private static string BuildReservationCreatedPayload(
        Guid eventId,
        Guid roomId,
        Guid reservationId,
        string reservedBy = "Phase 4 Reservee")
    {
        ReservationCreatedIntegrationEvent integrationEvent = new(
            EventId: eventId,
            OccurredUtc: new DateTimeOffset(2026, 4, 11, 10, 0, 0, TimeSpan.Zero),
            ReservationId: reservationId,
            RoomId: roomId,
            ReservedBy: reservedBy,
            Purpose: "Phase 4 test meeting",
            StartUtc: new DateTimeOffset(2026, 4, 12, 9, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero),
            CreatedUtc: new DateTimeOffset(2026, 4, 11, 10, 0, 0, TimeSpan.Zero),
            Status: "Confirmed",
            CorrelationId: null);

        return IntegrationEventJsonSerializer.Serialize(integrationEvent);
    }

    private sealed class ProcessorTestHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ReportingDbContext _dbContext;
        private readonly ReportingReservationMessageProcessor _processor;

        private ProcessorTestHarness(
            SqliteConnection connection,
            ReportingDbContext dbContext,
            ReportingReservationMessageProcessor processor)
        {
            this._connection = connection;
            this._dbContext = dbContext;
            this._processor = processor;
        }

        public static async Task<ProcessorTestHarness> CreateAsync()
        {
            SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<ReportingDbContext> options = new DbContextOptionsBuilder<ReportingDbContext>()
                .UseSqlite(connection)
                .Options;

            ReportingDbContext dbContext = new(options);
            await dbContext.Database.EnsureCreatedAsync();

            await SeedTestRoomAsync(dbContext);

            FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 4, 11, 12, 0, 0, TimeSpan.Zero));
            ReportingReservationMessageProcessor processor = new(
                dbContext,
                timeProvider,
                NullLogger<ReportingReservationMessageProcessor>.Instance);

            return new ProcessorTestHarness(connection, dbContext, processor);
        }

        public Task<MessageProcessingOutcome> ProcessAsync(
            Guid messageId,
            string subject,
            string payloadJson,
            string? correlationId,
            int deliveryCount)
        {
            return this._processor.ProcessAsync(
                messageId,
                subject,
                payloadJson,
                correlationId,
                deliveryCount,
                CancellationToken.None);
        }

        public async Task<T[]> QueryAsync<T>(Func<ReportingDbContext, IQueryable<T>> query)
        {
            return await query(this._dbContext).ToArrayAsync(CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await this._dbContext.DisposeAsync();
            await this._connection.DisposeAsync();
        }

        private static async Task SeedTestRoomAsync(ReportingDbContext dbContext)
        {
            dbContext.Rooms.Add(new RoomEntity
            {
                Id = TestRoomId,
                Code = "AMS-BOARD-01",
                Name = "Amsterdam Boardroom",
                Location = "Amsterdam HQ - Floor 5",
                Capacity = 12,
                AmenitiesJson = "[\"Teams Room\",\"Whiteboard\"]"
            });

            await dbContext.SaveChangesAsync();
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
