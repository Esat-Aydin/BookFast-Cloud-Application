// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReadModelQueries.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.ReadModels;
using BookFast.API.Contracts.Rooms;
using BookFast.API.Domain;
using BookFast.API.Services;

using HotChocolate;
using HotChocolate.Types;

namespace BookFast.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public sealed class ReadModelQueries
{
    [GraphQLDescription("Returns availability across multiple rooms for a requested time window in a single query.")]
    public async Task<IReadOnlyList<RoomAvailabilityOverviewResponse>> GetRoomAvailabilityOverview(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string? location = null,
        int? minimumCapacity = null,
        bool onlyAvailable = false,
        AvailabilityOverviewSortOrder sortBy = AvailabilityOverviewSortOrder.RoomCodeAscending,
        int skip = 0,
        int first = 20,
        IBookFastCatalog catalog = default!,
        CancellationToken cancellationToken = default)
    {
        GraphQLQueryGuard.EnsureTimeRange(fromUtc, toUtc);
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureMinimumCapacity(minimumCapacity);

        (Room[] rooms, Reservation[] reservations) = await LoadRoomsAndReservationsAsync(catalog, cancellationToken);

        string? normalizedLocation = Normalize(location);
        IEnumerable<Room> filteredRooms = rooms;
        if (normalizedLocation is not null)
        {
            filteredRooms = filteredRooms.Where(room =>
                room.Location.Equals(normalizedLocation, StringComparison.OrdinalIgnoreCase));
        }

        if (minimumCapacity is int capacity)
        {
            filteredRooms = filteredRooms.Where(room => room.Capacity >= capacity);
        }

        ILookup<Guid, Reservation> conflictsByRoomId = reservations
            .Where(reservation => IsConfirmedConflict(reservation, fromUtc, toUtc))
            .OrderBy(reservation => reservation.StartUtc)
            .ToLookup(reservation => reservation.RoomId);

        IEnumerable<RoomAvailabilityOverviewResponse> overview = filteredRooms.Select(room =>
        {
            AvailabilityConflictResponse[] conflicts = [..conflictsByRoomId[room.Id]
                .Select(ApiContractMapper.MapAvailabilityConflict)];

            return new RoomAvailabilityOverviewResponse(
                room.Id,
                room.Code,
                room.Name,
                room.Location,
                room.Capacity,
                fromUtc,
                toUtc,
                conflicts.Length == 0,
                conflicts.Length,
                conflicts);
        });

        if (onlyAvailable)
        {
            overview = overview.Where(item => item.IsAvailable);
        }

        return [..ApplyAvailabilityOverviewSorting(overview, sortBy)
            .Skip(skip)
            .Take(first)];
    }

    [GraphQLDescription("Returns occupancy summaries grouped by location for a requested time window.")]
    public async Task<IReadOnlyList<LocationOccupancySummaryResponse>> GetOccupancyOverview(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        string? location = null,
        OccupancyOverviewSortOrder sortBy = OccupancyOverviewSortOrder.LocationAscending,
        int skip = 0,
        int first = 20,
        IBookFastCatalog catalog = default!,
        CancellationToken cancellationToken = default)
    {
        GraphQLQueryGuard.EnsureTimeRange(fromUtc, toUtc);
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);

        (Room[] rooms, Reservation[] reservations) = await LoadRoomsAndReservationsAsync(catalog, cancellationToken);

        string? normalizedLocation = Normalize(location);
        Room[] filteredRooms =
        [
            ..rooms.Where(room =>
                normalizedLocation is null ||
                room.Location.Equals(normalizedLocation, StringComparison.OrdinalIgnoreCase))
        ];

        Reservation[] activeReservations =
        [
            ..reservations.Where(reservation => IsConfirmedConflict(reservation, fromUtc, toUtc))
        ];

        IEnumerable<LocationOccupancySummaryResponse> summaries = filteredRooms
            .GroupBy(room => room.Location, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateLocationOccupancySummary(group.Key, group, activeReservations, fromUtc, toUtc));

        return [..ApplyOccupancyOverviewSorting(summaries, sortBy)
            .Skip(skip)
            .Take(first)];
    }

    private static LocationOccupancySummaryResponse CreateLocationOccupancySummary(
        string location,
        IEnumerable<Room> rooms,
        IReadOnlyCollection<Reservation> activeReservations,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc)
    {
        Room[] roomArray = [..rooms];
        HashSet<Guid> roomIds = roomArray.Select(room => room.Id).ToHashSet();
        Reservation[] reservationsForLocation =
        [
            ..activeReservations.Where(reservation => roomIds.Contains(reservation.RoomId))
        ];

        HashSet<Guid> reservedRoomIds = reservationsForLocation
            .Select(reservation => reservation.RoomId)
            .ToHashSet();

        int totalRooms = roomArray.Length;
        int reservedRooms = reservedRoomIds.Count;
        int totalCapacity = roomArray.Sum(room => room.Capacity);
        int reservedCapacity = roomArray
            .Where(room => reservedRoomIds.Contains(room.Id))
            .Sum(room => room.Capacity);

        return new LocationOccupancySummaryResponse(
            location,
            fromUtc,
            toUtc,
            totalRooms,
            reservedRooms,
            totalRooms - reservedRooms,
            totalCapacity,
            reservedCapacity,
            reservationsForLocation.Length,
            RoundRatio(reservedRooms, totalRooms),
            RoundRatio(reservedCapacity, totalCapacity));
    }

    private static async Task<(Room[] Rooms, Reservation[] Reservations)> LoadRoomsAndReservationsAsync(
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyCollection<Room>> roomsTask = catalog.ListRoomsAsync(cancellationToken);
        Task<IReadOnlyCollection<Reservation>> reservationsTask = catalog.ListReservationsAsync(cancellationToken);

        await Task.WhenAll(roomsTask, reservationsTask);

        IReadOnlyCollection<Room> rooms = await roomsTask;
        IReadOnlyCollection<Reservation> reservations = await reservationsTask;

        return ([..rooms], [..reservations]);
    }

    private static IOrderedEnumerable<RoomAvailabilityOverviewResponse> ApplyAvailabilityOverviewSorting(
        IEnumerable<RoomAvailabilityOverviewResponse> overview,
        AvailabilityOverviewSortOrder sortBy)
    {
        return sortBy switch
        {
            AvailabilityOverviewSortOrder.RoomCodeDescending => overview.OrderByDescending(
                item => item.RoomCode,
                StringComparer.OrdinalIgnoreCase),
            AvailabilityOverviewSortOrder.LocationAscending => overview.OrderBy(
                item => item.Location,
                StringComparer.OrdinalIgnoreCase),
            AvailabilityOverviewSortOrder.LocationDescending => overview.OrderByDescending(
                item => item.Location,
                StringComparer.OrdinalIgnoreCase),
            AvailabilityOverviewSortOrder.CapacityAscending => overview.OrderBy(item => item.Capacity),
            AvailabilityOverviewSortOrder.CapacityDescending => overview.OrderByDescending(item => item.Capacity),
            AvailabilityOverviewSortOrder.AvailabilityAscending => overview
                .OrderBy(item => item.IsAvailable)
                .ThenBy(item => item.RoomCode, StringComparer.OrdinalIgnoreCase),
            AvailabilityOverviewSortOrder.AvailabilityDescending => overview
                .OrderByDescending(item => item.IsAvailable)
                .ThenBy(item => item.RoomCode, StringComparer.OrdinalIgnoreCase),
            _ => overview.OrderBy(item => item.RoomCode, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static IOrderedEnumerable<LocationOccupancySummaryResponse> ApplyOccupancyOverviewSorting(
        IEnumerable<LocationOccupancySummaryResponse> summaries,
        OccupancyOverviewSortOrder sortBy)
    {
        return sortBy switch
        {
            OccupancyOverviewSortOrder.LocationDescending => summaries.OrderByDescending(
                item => item.Location,
                StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.RoomOccupancyRateAscending => summaries
                .OrderBy(item => item.RoomOccupancyRate)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.RoomOccupancyRateDescending => summaries
                .OrderByDescending(item => item.RoomOccupancyRate)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.CapacityOccupancyRateAscending => summaries
                .OrderBy(item => item.CapacityOccupancyRate)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.CapacityOccupancyRateDescending => summaries
                .OrderByDescending(item => item.CapacityOccupancyRate)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.ReservedRoomsAscending => summaries
                .OrderBy(item => item.ReservedRooms)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            OccupancyOverviewSortOrder.ReservedRoomsDescending => summaries
                .OrderByDescending(item => item.ReservedRooms)
                .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase),
            _ => summaries.OrderBy(item => item.Location, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool IsConfirmedConflict(Reservation reservation, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        return reservation.Status == ReservationStatus.Confirmed &&
               reservation.StartUtc < toUtc &&
               reservation.EndUtc > fromUtc;
    }

    private static double RoundRatio(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            return 0;
        }

        return Math.Round((double)numerator / denominator, 4, MidpointRounding.AwayFromZero);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
