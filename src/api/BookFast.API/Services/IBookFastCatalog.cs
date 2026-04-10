// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IBookFastCatalog.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

namespace BookFast.API.Services;

public interface IBookFastCatalog
{
    Task<IReadOnlyCollection<Room>> ListRoomsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Room>> ListRoomsByIdsAsync(
        IEnumerable<Guid> roomIds,
        CancellationToken cancellationToken);

    Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Reservation>> ListReservationsAsync(CancellationToken cancellationToken);

    Task<Reservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    Task<AvailabilityCheckResult> CheckAvailabilityAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken);

    Task<ReservationCreationResult> CreateReservationAsync(
        Guid roomId,
        string reservedBy,
        string? purpose,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? correlationId,
        CancellationToken cancellationToken);
}
