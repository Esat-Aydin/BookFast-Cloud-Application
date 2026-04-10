// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : SqlBookFastCatalogTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Domain;
using BookFast.API.Infrastructure.Persistence;
using BookFast.API.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookFast.API.Tests;

public sealed class SqlBookFastCatalogTests
{
    [Fact]
    public async Task ListRoomsAsync_ShouldReturnSeededRooms()
    {
        await using SqlBookFastCatalogTestHarness harness = await SqlBookFastCatalogTestHarness.CreateAsync(
            new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));

        IReadOnlyCollection<Room> rooms = await harness.Catalog.ListRoomsAsync(CancellationToken.None);

        Assert.Equal(3, rooms.Count);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldCreateReservation_WhenSlotIsAvailable()
    {
        await using SqlBookFastCatalogTestHarness harness = await SqlBookFastCatalogTestHarness.CreateAsync(
            new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));

        IReadOnlyCollection<Room> rooms = await harness.Catalog.ListRoomsAsync(CancellationToken.None);
        Room room = rooms.First();
        DateTimeOffset startUtc = harness.TimeProvider.GetUtcNow().AddHours(2);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        ReservationCreationResult result = await harness.Catalog.CreateReservationAsync(
            room.Id,
            "Recruiter Demo",
            "Room reservation flow",
            startUtc,
            endUtc,
            CancellationToken.None);

        IReadOnlyCollection<Reservation> reservations = await harness.Catalog.ListReservationsAsync(CancellationToken.None);

        Assert.Equal(ReservationCreationStatus.Created, result.Status);
        Assert.NotNull(result.Reservation);
        Assert.Single(reservations);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldReturnConflict_WhenReservationOverlaps()
    {
        await using SqlBookFastCatalogTestHarness harness = await SqlBookFastCatalogTestHarness.CreateAsync(
            new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));

        IReadOnlyCollection<Room> rooms = await harness.Catalog.ListRoomsAsync(CancellationToken.None);
        Room room = rooms.First();
        DateTimeOffset firstStartUtc = harness.TimeProvider.GetUtcNow().AddHours(2);
        DateTimeOffset firstEndUtc = firstStartUtc.AddHours(1);
        DateTimeOffset overlappingStartUtc = firstStartUtc.AddMinutes(30);
        DateTimeOffset overlappingEndUtc = firstEndUtc.AddMinutes(30);

        ReservationCreationResult firstReservation = await harness.Catalog.CreateReservationAsync(
            room.Id,
            "Planner",
            "First reservation",
            firstStartUtc,
            firstEndUtc,
            CancellationToken.None);

        ReservationCreationResult secondReservation = await harness.Catalog.CreateReservationAsync(
            room.Id,
            "Planner",
            "Overlapping reservation",
            overlappingStartUtc,
            overlappingEndUtc,
            CancellationToken.None);

        Assert.Equal(ReservationCreationStatus.Created, firstReservation.Status);
        Assert.Equal(ReservationCreationStatus.Conflict, secondReservation.Status);
        Assert.Single(secondReservation.ConflictingReservations);
    }

    [Fact]
    public async Task CreateReservationAsync_ShouldRejectStartTimeInPast()
    {
        await using SqlBookFastCatalogTestHarness harness = await SqlBookFastCatalogTestHarness.CreateAsync(
            new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));

        IReadOnlyCollection<Room> rooms = await harness.Catalog.ListRoomsAsync(CancellationToken.None);
        Room room = rooms.First();
        DateTimeOffset startUtc = harness.TimeProvider.GetUtcNow().AddMinutes(-30);
        DateTimeOffset endUtc = harness.TimeProvider.GetUtcNow().AddHours(1);

        ReservationCreationResult result = await harness.Catalog.CreateReservationAsync(
            room.Id,
            "Planner",
            "Past reservation",
            startUtc,
            endUtc,
            CancellationToken.None);

        IReadOnlyCollection<Reservation> reservations = await harness.Catalog.ListReservationsAsync(CancellationToken.None);

        Assert.Equal(ReservationCreationStatus.StartTimeInPast, result.Status);
        Assert.Empty(reservations);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ShouldReturnUnavailable_WhenConflictExists()
    {
        await using SqlBookFastCatalogTestHarness harness = await SqlBookFastCatalogTestHarness.CreateAsync(
            new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));

        IReadOnlyCollection<Room> rooms = await harness.Catalog.ListRoomsAsync(CancellationToken.None);
        Room room = rooms.First();
        DateTimeOffset startUtc = harness.TimeProvider.GetUtcNow().AddHours(3);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        await harness.Catalog.CreateReservationAsync(
            room.Id,
            "Planner",
            "Reservation for availability check",
            startUtc,
            endUtc,
            CancellationToken.None);

        AvailabilityCheckResult availability = await harness.Catalog.CheckAvailabilityAsync(
            room.Id,
            startUtc,
            endUtc,
            CancellationToken.None);

        Assert.True(availability.RoomExists);
        Assert.True(availability.TimeRangeValid);
        Assert.False(availability.IsAvailable);
        Assert.Single(availability.ConflictingReservations);
    }

    private sealed class SqlBookFastCatalogTestHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly BookFastDbContext _dbContext;

        private SqlBookFastCatalogTestHarness(
            SqliteConnection connection,
            BookFastDbContext dbContext,
            FixedTimeProvider timeProvider)
        {
            this._connection = connection;
            this._dbContext = dbContext;
            this.TimeProvider = timeProvider;
            this.Catalog = new SqlBookFastCatalog(dbContext, timeProvider);
        }

        public SqlBookFastCatalog Catalog { get; }

        public FixedTimeProvider TimeProvider { get; }

        public static async Task<SqlBookFastCatalogTestHarness> CreateAsync(DateTimeOffset utcNow)
        {
            SqliteConnection connection = new("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<BookFastDbContext> options = new DbContextOptionsBuilder<BookFastDbContext>()
                .UseSqlite(connection)
                .Options;

            BookFastDbContext dbContext = new(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new SqlBookFastCatalogTestHarness(connection, dbContext, new FixedTimeProvider(utcNow));
        }

        public async ValueTask DisposeAsync()
        {
            await this._dbContext.DisposeAsync();
            await this._connection.DisposeAsync();
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
