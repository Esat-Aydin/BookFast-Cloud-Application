// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiContractMapper.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Contracts.Reservations;
using BookFast.API.Contracts.Rooms;
using BookFast.API.Domain;
using BookFast.API.Services;

namespace BookFast.API.Common;

public static class ApiContractMapper
{
    public static RoomResponse MapRoom(Room room)
    {
        return new RoomResponse(
            room.Id,
            room.Code,
            room.Name,
            room.Location,
            room.Capacity,
            room.Amenities);
    }

    public static AvailabilityConflictResponse MapAvailabilityConflict(Reservation reservation)
    {
        return new AvailabilityConflictResponse(
            reservation.Id,
            reservation.ReservedBy,
            reservation.StartUtc,
            reservation.EndUtc,
            reservation.Status.ToString());
    }

    public static RoomAvailabilityResponse MapAvailability(
        Room room,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        AvailabilityCheckResult result)
    {
        AvailabilityConflictResponse[] conflicts = [..result.ConflictingReservations.Select(MapAvailabilityConflict)];

        return new RoomAvailabilityResponse(
            room.Id,
            room.Code,
            room.Name,
            fromUtc,
            toUtc,
            result.IsAvailable,
            conflicts);
    }

    public static ReservationResponse? MapReservation(Reservation reservation, IBookFastCatalog catalog)
    {
        Room? room = catalog.GetRoom(reservation.RoomId);
        if (room is null)
        {
            return null;
        }

        return new ReservationResponse(
            reservation.Id,
            reservation.RoomId,
            room.Code,
            room.Name,
            reservation.ReservedBy,
            reservation.Purpose,
            reservation.StartUtc,
            reservation.EndUtc,
            reservation.CreatedUtc,
            reservation.Status.ToString());
    }
}
