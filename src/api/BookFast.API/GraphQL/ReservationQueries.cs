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

        string? normalizedLocation = Normalize(location);
        if (normalizedLocation is not null)
        {
            filteredReservations = filteredReservations.Where(reservation =>
                roomsById.TryGetValue(reservation.RoomId, out Room? room) &&
                room.Location.Equals(normalizedLocation, StringComparison.OrdinalIgnoreCase));
        }

        string? normalizedReservedBy = Normalize(reservedByContains);
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

        Reservation[] page = [..ApplySorting(filteredReservations, sortBy)
            .Skip(skip)
            .Take(first)];

        return [..page
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

    private static IOrderedEnumerable<Reservation> ApplySorting(
        IEnumerable<Reservation> reservations,
        ReservationSortOrder sortBy)
    {
        return sortBy switch
        {
            ReservationSortOrder.StartUtcDescending => reservations.OrderByDescending(reservation => reservation.StartUtc),
            ReservationSortOrder.EndUtcAscending => reservations.OrderBy(reservation => reservation.EndUtc),
            ReservationSortOrder.EndUtcDescending => reservations.OrderByDescending(reservation => reservation.EndUtc),
            ReservationSortOrder.CreatedUtcAscending => reservations.OrderBy(reservation => reservation.CreatedUtc),
            ReservationSortOrder.CreatedUtcDescending => reservations.OrderByDescending(reservation => reservation.CreatedUtc),
            ReservationSortOrder.ReservedByAscending => reservations.OrderBy(
                reservation => reservation.ReservedBy,
                StringComparer.OrdinalIgnoreCase),
            ReservationSortOrder.ReservedByDescending => reservations.OrderByDescending(
                reservation => reservation.ReservedBy,
                StringComparer.OrdinalIgnoreCase),
            _ => reservations.OrderBy(reservation => reservation.StartUtc)
        };
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
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
}
