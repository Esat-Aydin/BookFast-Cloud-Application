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
    IReadOnlyCollection<Room> ListRooms();

    Room? GetRoom(Guid roomId);

    IReadOnlyCollection<Reservation> ListReservations();

    Reservation? GetReservation(Guid reservationId);

    AvailabilityCheckResult CheckAvailability(Guid roomId, DateTimeOffset fromUtc, DateTimeOffset toUtc);

    ReservationCreationResult CreateReservation(
        Guid roomId,
        string reservedBy,
        string? purpose,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc);
}
