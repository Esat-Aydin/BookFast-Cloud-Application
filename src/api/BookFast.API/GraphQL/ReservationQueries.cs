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
    public IReadOnlyList<ReservationResponse> GetReservations(
        IBookFastCatalog catalog,
        Guid? roomId = null,
        string? reservedByContains = null,
        ReservationStatus? status = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        int skip = 0,
        int first = 20)
    {
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureOptionalTimeRange(fromUtc, toUtc);

        IEnumerable<Reservation> reservations = catalog.ListReservations();
        if (roomId.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.RoomId == roomId.Value);
        }

        string? normalizedReservedBy = Normalize(reservedByContains);
        if (normalizedReservedBy is not null)
        {
            reservations = reservations.Where(reservation =>
                reservation.ReservedBy.Contains(normalizedReservedBy, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.Status == status.Value);
        }

        if (fromUtc.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.EndUtc > fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.StartUtc < toUtc.Value);
        }

        return [..reservations
            .Skip(skip)
            .Take(first)
            .Select(reservation => ApiContractMapper.MapReservation(reservation, catalog))
            .Where(response => response is not null)
            .Select(response => response!)];
    }

    public ReservationResponse? GetReservation(Guid id, IBookFastCatalog catalog)
    {
        Reservation? reservation = catalog.GetReservation(id);
        if (reservation is null)
        {
            return null;
        }

        return ApiContractMapper.MapReservation(reservation, catalog);
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
