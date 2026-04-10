// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationCreationResult.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

namespace BookFast.API.Services;

public sealed class ReservationCreationResult
{
    private ReservationCreationResult(
        ReservationCreationStatus status,
        Reservation? reservation,
        IReadOnlyCollection<Reservation> conflictingReservations)
    {
        this.Status = status;
        this.Reservation = reservation;
        this.ConflictingReservations = conflictingReservations;
    }

    public ReservationCreationStatus Status { get; }

    public Reservation? Reservation { get; }

    public IReadOnlyCollection<Reservation> ConflictingReservations { get; }

    public static ReservationCreationResult Created(Reservation reservation)
    {
        return new ReservationCreationResult(
            ReservationCreationStatus.Created,
            reservation,
            []);
    }

    public static ReservationCreationResult RoomNotFound()
    {
        return new ReservationCreationResult(
            ReservationCreationStatus.RoomNotFound,
            null,
            []);
    }

    public static ReservationCreationResult InvalidTimeRange()
    {
        return new ReservationCreationResult(
            ReservationCreationStatus.InvalidTimeRange,
            null,
            []);
    }

    public static ReservationCreationResult StartTimeInPast()
    {
        return new ReservationCreationResult(
            ReservationCreationStatus.StartTimeInPast,
            null,
            []);
    }

    public static ReservationCreationResult Conflict(IReadOnlyCollection<Reservation> conflictingReservations)
    {
        return new ReservationCreationResult(
            ReservationCreationStatus.Conflict,
            null,
            conflictingReservations);
    }
}
