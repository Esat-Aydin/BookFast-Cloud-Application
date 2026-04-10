// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : InMemoryBookFastCatalog.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

namespace BookFast.API.Services;

public sealed class InMemoryBookFastCatalog : IBookFastCatalog
{
    private readonly Lock _syncRoot;
    private readonly TimeProvider _timeProvider;
    private readonly List<Room> _rooms;
    private readonly List<Reservation> _reservations;

    public InMemoryBookFastCatalog(TimeProvider timeProvider)
    {
        this._syncRoot = new Lock();
        this._timeProvider = timeProvider;
        this._rooms = CreateSeedRooms();
        this._reservations = [];
    }

    public IReadOnlyCollection<Room> ListRooms()
    {
        lock (this._syncRoot)
        {
            Room[] rooms = [..this._rooms.OrderBy(room => room.Code)];

            return rooms;
        }
    }

    public Room? GetRoom(Guid roomId)
    {
        lock (this._syncRoot)
        {
            Room? room = this._rooms.SingleOrDefault(candidate => candidate.Id == roomId);
            return room;
        }
    }

    public IReadOnlyCollection<Reservation> ListReservations()
    {
        lock (this._syncRoot)
        {
            Reservation[] reservations = [..this._reservations.OrderBy(reservation => reservation.StartUtc)];

            return reservations;
        }
    }

    public Reservation? GetReservation(Guid reservationId)
    {
        lock (this._syncRoot)
        {
            Reservation? reservation = this._reservations.SingleOrDefault(candidate => candidate.Id == reservationId);
            return reservation;
        }
    }

    public AvailabilityCheckResult CheckAvailability(Guid roomId, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        if (fromUtc == default || toUtc == default)
        {
            return AvailabilityCheckResult.InvalidTimeRange();
        }

        if (fromUtc >= toUtc)
        {
            return AvailabilityCheckResult.InvalidTimeRange();
        }

        lock (this._syncRoot)
        {
            Room? room = this._rooms.SingleOrDefault(candidate => candidate.Id == roomId);
            if (room is null)
            {
                return AvailabilityCheckResult.RoomNotFound();
            }

            Reservation[] conflicts = this.GetConflicts(roomId, fromUtc, toUtc);
            if (conflicts.Length == 0)
            {
                return AvailabilityCheckResult.Available();
            }

            return AvailabilityCheckResult.Unavailable(conflicts);
        }
    }

    public ReservationCreationResult CreateReservation(
        Guid roomId,
        string reservedBy,
        string? purpose,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        if (string.IsNullOrWhiteSpace(reservedBy))
        {
            throw new ArgumentException("reservedBy must be provided.", nameof(reservedBy));
        }

        if (startUtc == default || endUtc == default)
        {
            return ReservationCreationResult.InvalidTimeRange();
        }

        if (startUtc >= endUtc)
        {
            return ReservationCreationResult.InvalidTimeRange();
        }

        DateTimeOffset now = this._timeProvider.GetUtcNow();
        if (startUtc < now)
        {
            return ReservationCreationResult.StartTimeInPast();
        }

        lock (this._syncRoot)
        {
            Room? room = this._rooms.SingleOrDefault(candidate => candidate.Id == roomId);
            if (room is null)
            {
                return ReservationCreationResult.RoomNotFound();
            }

            Reservation[] conflicts = this.GetConflicts(roomId, startUtc, endUtc);
            if (conflicts.Length > 0)
            {
                return ReservationCreationResult.Conflict(conflicts);
            }

            Reservation reservation = new(
                Guid.NewGuid(),
                roomId,
                reservedBy.Trim(),
                NormalizeOptionalText(purpose),
                startUtc,
                endUtc,
                now,
                ReservationStatus.Confirmed);

            this._reservations.Add(reservation);

            return ReservationCreationResult.Created(reservation);
        }
    }

    private static List<Room> CreateSeedRooms()
    {
        return
        [
            new(
                Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901"),
                "AMS-BOARD-01",
                "Amsterdam Boardroom",
                "Amsterdam HQ - Floor 5",
                12,
                ["Teams Room", "Whiteboard", "4K Display"]),
            new(
                Guid.Parse("A8B70B66-676C-4A1D-9EA6-865A0B918A72"),
                "UTR-COLLAB-02",
                "Utrecht Collaboration Hub",
                "Utrecht Office - Floor 2",
                8,
                ["Video Conferencing", "Whiteboard"]),
            new(
                Guid.Parse("C93B8E11-6D12-4F7C-8BF0-08FA0A1D2C54"),
                "RTM-FOCUS-03",
                "Rotterdam Focus Room",
                "Rotterdam Office - Floor 3",
                4,
                ["Quiet Zone", "Docking Station"])
        ];
    }

    private Reservation[] GetConflicts(Guid roomId, DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        Reservation[] conflicts = [..this._reservations
                                      .Where(reservation => reservation.RoomId == roomId)
                                      .Where(reservation => reservation.Status == ReservationStatus.Confirmed)
                                      .Where(reservation => reservation.StartUtc < toUtc && reservation.EndUtc > fromUtc)
                                      .OrderBy(reservation => reservation.StartUtc)];

        return conflicts;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
