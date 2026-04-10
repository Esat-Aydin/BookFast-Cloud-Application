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

namespace BookFast.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public sealed class ReservationQueries
{
    public async Task<IReadOnlyList<ReservationResponse>> GetReservations(
        Guid? roomId = null,
        string? reservedByContains = null,
        ReservationStatus? status = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        int skip = 0,
        int first = 20,
        IBookFastCatalog catalog = default!,
        CancellationToken cancellationToken = default)
    {
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureOptionalTimeRange(fromUtc, toUtc);

        IReadOnlyCollection<Reservation> reservations = await catalog.ListReservationsAsync(cancellationToken);
        IEnumerable<Reservation> filteredReservations = reservations;
        if (roomId.HasValue)
        {
            filteredReservations = filteredReservations.Where(reservation => reservation.RoomId == roomId.Value);
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

        Reservation[] page = [..filteredReservations
            .Skip(skip)
            .Take(first)];

        Guid[] roomIds = [..page
            .Select(reservation => reservation.RoomId)
            .Distinct()];

        IReadOnlyDictionary<Guid, Room> roomsById = await catalog.ListRoomsByIdsAsync(roomIds, cancellationToken);

        return [..page
            .Select(reservation => TryMapReservation(reservation, roomsById))
            .Where(response => response is not null)
            .Select(response => response!)];
    }

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
