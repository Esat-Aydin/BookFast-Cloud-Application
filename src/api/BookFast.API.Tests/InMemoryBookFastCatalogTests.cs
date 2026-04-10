// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : InMemoryBookFastCatalogTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Domain;
using BookFast.API.Services;

namespace BookFast.API.Tests;

public sealed class InMemoryBookFastCatalogTests
{
    [Fact]
    public void ListRooms_ShouldReturnSeededRooms()
    {
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
        InMemoryBookFastCatalog catalog = new InMemoryBookFastCatalog(timeProvider);

        IReadOnlyCollection<Room> rooms = catalog.ListRooms();

        Assert.Equal(3, rooms.Count);
    }

    [Fact]
    public void CreateReservation_ShouldCreateReservation_WhenSlotIsAvailable()
    {
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
        InMemoryBookFastCatalog catalog = new InMemoryBookFastCatalog(timeProvider);
        Room room = catalog.ListRooms().First();
        DateTimeOffset startUtc = timeProvider.GetUtcNow().AddHours(2);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        ReservationCreationResult result = catalog.CreateReservation(
            room.Id,
            "Recruiter Demo",
            "Room reservation flow",
            startUtc,
            endUtc);

        Assert.Equal(ReservationCreationStatus.Created, result.Status);
        Assert.NotNull(result.Reservation);
        Assert.Single(catalog.ListReservations());
    }

    [Fact]
    public void CreateReservation_ShouldReturnConflict_WhenReservationOverlaps()
    {
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
        InMemoryBookFastCatalog catalog = new InMemoryBookFastCatalog(timeProvider);
        Room room = catalog.ListRooms().First();
        DateTimeOffset firstStartUtc = timeProvider.GetUtcNow().AddHours(2);
        DateTimeOffset firstEndUtc = firstStartUtc.AddHours(1);
        DateTimeOffset overlappingStartUtc = firstStartUtc.AddMinutes(30);
        DateTimeOffset overlappingEndUtc = firstEndUtc.AddMinutes(30);

        ReservationCreationResult firstReservation = catalog.CreateReservation(
            room.Id,
            "Planner",
            "First reservation",
            firstStartUtc,
            firstEndUtc);

        ReservationCreationResult secondReservation = catalog.CreateReservation(
            room.Id,
            "Planner",
            "Overlapping reservation",
            overlappingStartUtc,
            overlappingEndUtc);

        Assert.Equal(ReservationCreationStatus.Created, firstReservation.Status);
        Assert.Equal(ReservationCreationStatus.Conflict, secondReservation.Status);
        Assert.Single(secondReservation.ConflictingReservations);
    }

    [Fact]
    public void CreateReservation_ShouldRejectStartTimeInPast()
    {
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
        InMemoryBookFastCatalog catalog = new InMemoryBookFastCatalog(timeProvider);
        Room room = catalog.ListRooms().First();
        DateTimeOffset startUtc = timeProvider.GetUtcNow().AddMinutes(-30);
        DateTimeOffset endUtc = timeProvider.GetUtcNow().AddHours(1);

        ReservationCreationResult result = catalog.CreateReservation(
            room.Id,
            "Planner",
            "Past reservation",
            startUtc,
            endUtc);

        Assert.Equal(ReservationCreationStatus.StartTimeInPast, result.Status);
        Assert.Empty(catalog.ListReservations());
    }

    [Fact]
    public void CheckAvailability_ShouldReturnUnavailable_WhenConflictExists()
    {
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 4, 10, 8, 0, 0, TimeSpan.Zero));
        InMemoryBookFastCatalog catalog = new InMemoryBookFastCatalog(timeProvider);
        Room room = catalog.ListRooms().First();
        DateTimeOffset startUtc = timeProvider.GetUtcNow().AddHours(3);
        DateTimeOffset endUtc = startUtc.AddHours(1);

        catalog.CreateReservation(
            room.Id,
            "Planner",
            "Reservation for availability check",
            startUtc,
            endUtc);

        AvailabilityCheckResult availability = catalog.CheckAvailability(room.Id, startUtc, endUtc);

        Assert.True(availability.RoomExists);
        Assert.True(availability.TimeRangeValid);
        Assert.False(availability.IsAvailable);
        Assert.Single(availability.ConflictingReservations);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
