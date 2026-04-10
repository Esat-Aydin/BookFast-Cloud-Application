// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationQueries.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.Reservations;
using BookFast.API.Domain;
using BookFast.API.Services;

using HotChocolate;

namespace BookFast.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public sealed class ReservationQueries
{
    [GraphQLDescription("Returns reservation read models for consumer-facing query scenarios.")]
    public async Task<IReadOnlyList<ReservationResponse>> GetReservations(
        Guid? roomId = null,
        string? location = null,
        string? reservedByContains = null,
        ReservationStatus? status = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        ReservationSortOrder sortBy = ReservationSortOrder.StartUtcAscending,
        int skip = 0,
        int first = 20,
        IBookFastCatalog catalog = default!,
        CancellationToken cancellationToken = default)
    {
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureOptionalTimeRange(fromUtc, toUtc);

        Task<IReadOnlyCollection<Reservation>> reservationsTask = catalog.ListReservationsAsync(cancellationToken);
        Task<IReadOnlyCollection<Room>> roomsTask = catalog.ListRoomsAsync(cancellationToken);

        await Task.WhenAll(reservationsTask, roomsTask);

        IReadOnlyCollection<Reservation> reservations = await reservationsTask;
        IReadOnlyCollection<Room> rooms = await roomsTask;
        IReadOnlyDictionary<Guid, Room> roomsById = rooms.ToDictionary(room => room.Id);

        IEnumerable<Reservation> filteredReservations = reservations;
        if (roomId.HasValue)
        {
            filteredReservations = filteredReservations.Where(reservation => reservation.RoomId == roomId.Value);
        }

        string? normalizedReservedBy = GraphQLQueryGuard.NormalizeOptionalText(reservedByContains);
        if (normalizedReservedBy is not null)
        {
            filteredReservations = filteredReservations.Where(reservation =>
                reservation.ReservedBy.Contains(normalizedReservedBy, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            filteredReservations = filteredReservations.Where(reservation => reservation.Status == status.Value);
        }

        if (fromUtc.HasValue)
        {
            filteredReservations = filteredReservations.Where(reservation => reservation.EndUtc > fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            filteredReservations = filteredReservations.Where(reservation => reservation.StartUtc < toUtc.Value);
        }

        string? normalizedLocation = GraphQLQueryGuard.NormalizeOptionalText(location);

        return [..ApplySorting(filteredReservations, sortBy)
            .Where(reservation => MatchesLocationFilter(reservation, roomsById, normalizedLocation))
            .Skip(skip)
            .Take(first)
            .Select(reservation => TryMapReservation(reservation, roomsById))
            .Where(response => response is not null)
            .Select(response => response!)];
    }

    [GraphQLDescription("Returns one reservation by identifier.")]
    public async Task<ReservationResponse?> GetReservation(
        Guid id,
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        Reservation? reservation = await catalog.GetReservationAsync(id, cancellationToken);
        if (reservation is null)
        {
            return null;
        }

        Room? room = await catalog.GetRoomAsync(reservation.RoomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        return ApiContractMapper.MapReservation(reservation, room);
    }

    private static IEnumerable<Reservation> ApplySorting(
        IEnumerable<Reservation> reservations,
        ReservationSortOrder sortBy)
    {
        return sortBy switch
        {
            ReservationSortOrder.StartUtcDescending => reservations
                .OrderByDescending(reservation => reservation.StartUtc)
                .ThenByDescending(reservation => reservation.CreatedUtc),
            ReservationSortOrder.EndUtcAscending => reservations
                .OrderBy(reservation => reservation.EndUtc)
                .ThenBy(reservation => reservation.StartUtc),
            ReservationSortOrder.EndUtcDescending => reservations
                .OrderByDescending(reservation => reservation.EndUtc)
                .ThenByDescending(reservation => reservation.StartUtc),
            ReservationSortOrder.CreatedUtcAscending => reservations
                .OrderBy(reservation => reservation.CreatedUtc)
                .ThenBy(reservation => reservation.StartUtc),
            ReservationSortOrder.CreatedUtcDescending => reservations
                .OrderByDescending(reservation => reservation.CreatedUtc)
                .ThenByDescending(reservation => reservation.StartUtc),
            ReservationSortOrder.ReservedByAscending => reservations
                .OrderBy(reservation => reservation.ReservedBy, StringComparer.OrdinalIgnoreCase)
                .ThenBy(reservation => reservation.StartUtc),
            ReservationSortOrder.ReservedByDescending => reservations
                .OrderByDescending(reservation => reservation.ReservedBy, StringComparer.OrdinalIgnoreCase)
                .ThenBy(reservation => reservation.StartUtc),
            _ => reservations.OrderBy(reservation => reservation.StartUtc).ThenBy(reservation => reservation.CreatedUtc)
        };
    }

    private static ReservationResponse? TryMapReservation(
        Reservation reservation,
        IReadOnlyDictionary<Guid, Room> roomsById)
    {
        if (!roomsById.TryGetValue(reservation.RoomId, out Room? room))
        {
            return null;
        }

        return ApiContractMapper.MapReservation(reservation, room);
    }

    private static bool MatchesLocationFilter(
        Reservation reservation,
        IReadOnlyDictionary<Guid, Room> roomsById,
        string? normalizedLocation)
    {
        if (normalizedLocation is null)
        {
            return true;
        }

        return roomsById.TryGetValue(reservation.RoomId, out Room? room) &&
               room.Location.Equals(normalizedLocation, StringComparison.OrdinalIgnoreCase);
    }
}
