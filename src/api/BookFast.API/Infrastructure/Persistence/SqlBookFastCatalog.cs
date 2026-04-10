// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : SqlBookFastCatalog.cs
//  Project         : BookFast.API
// ******************************************************************************

using System.Data;
using System.Text.Json;

using BookFast.API.Domain;
using BookFast.API.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookFast.API.Infrastructure.Persistence;

public sealed class SqlBookFastCatalog : IBookFastCatalog
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly BookFastDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SqlBookFastCatalog(BookFastDbContext dbContext, TimeProvider timeProvider)
    {
        this._dbContext = dbContext;
        this._timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<Room>> ListRoomsAsync(CancellationToken cancellationToken)
    {
        RoomEntity[] rooms = await this._dbContext.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Code)
            .ToArrayAsync(cancellationToken);

        return [..rooms.Select(MapRoom)];
    }

    public async Task<IReadOnlyDictionary<Guid, Room>> ListRoomsByIdsAsync(
        IEnumerable<Guid> roomIds,
        CancellationToken cancellationToken)
    {
        Guid[] requestedRoomIds = [..roomIds.Distinct()];
        if (requestedRoomIds.Length == 0)
        {
            return new Dictionary<Guid, Room>();
        }

        RoomEntity[] rooms = await this._dbContext.Rooms
            .AsNoTracking()
            .Where(room => requestedRoomIds.Contains(room.Id))
            .ToArrayAsync(cancellationToken);

        return rooms.ToDictionary(room => room.Id, MapRoom);
    }

    public async Task<Room?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        RoomEntity? room = await this._dbContext.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == roomId, cancellationToken);

        return room is null ? null : MapRoom(room);
    }

    public async Task<IReadOnlyCollection<Reservation>> ListReservationsAsync(CancellationToken cancellationToken)
    {
        ReservationEntity[] reservations = await this._dbContext.Reservations
            .AsNoTracking()
            .OrderBy(reservation => reservation.StartUtc)
            .ToArrayAsync(cancellationToken);

        return [..reservations.Select(MapReservation)];
    }

    public async Task<Reservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        ReservationEntity? reservation = await this._dbContext.Reservations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == reservationId, cancellationToken);

        return reservation is null ? null : MapReservation(reservation);
    }

    public async Task<AvailabilityCheckResult> CheckAvailabilityAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc == default || toUtc == default || fromUtc >= toUtc)
        {
            return AvailabilityCheckResult.InvalidTimeRange();
        }

        bool roomExists = await this._dbContext.Rooms
            .AsNoTracking()
            .AnyAsync(room => room.Id == roomId, cancellationToken);

        if (!roomExists)
        {
            return AvailabilityCheckResult.RoomNotFound();
        }

        ReservationEntity[] conflicts = await this.GetConflictsAsync(roomId, fromUtc, toUtc, cancellationToken);
        if (conflicts.Length == 0)
        {
            return AvailabilityCheckResult.Available();
        }

        return AvailabilityCheckResult.Unavailable([..conflicts.Select(MapReservation)]);
    }

    public async Task<ReservationCreationResult> CreateReservationAsync(
        Guid roomId,
        string reservedBy,
        string? purpose,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservedBy))
        {
            throw new ArgumentException("reservedBy must be provided.", nameof(reservedBy));
        }

        if (startUtc == default || endUtc == default || startUtc >= endUtc)
        {
            return ReservationCreationResult.InvalidTimeRange();
        }

        DateTimeOffset now = this._timeProvider.GetUtcNow();
        if (startUtc < now)
        {
            return ReservationCreationResult.StartTimeInPast();
        }

        await using IDbContextTransaction transaction = await this._dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        bool roomExists = await this._dbContext.Rooms
            .AnyAsync(room => room.Id == roomId, cancellationToken);

        if (!roomExists)
        {
            return ReservationCreationResult.RoomNotFound();
        }

        ReservationEntity[] conflicts = await this.GetConflictsAsync(roomId, startUtc, endUtc, cancellationToken);
        if (conflicts.Length > 0)
        {
            return ReservationCreationResult.Conflict([..conflicts.Select(MapReservation)]);
        }

        ReservationEntity reservation = new()
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            ReservedBy = reservedBy.Trim(),
            Purpose = NormalizeOptionalText(purpose),
            StartUtc = startUtc.UtcDateTime,
            EndUtc = endUtc.UtcDateTime,
            CreatedUtc = now.UtcDateTime,
            Status = ReservationStatus.Confirmed
        };

        await this._dbContext.Reservations.AddAsync(reservation, cancellationToken);
        await this._dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ReservationCreationResult.Created(MapReservation(reservation));
    }

    private async Task<ReservationEntity[]> GetConflictsAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        return await this._dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.RoomId == roomId)
            .Where(reservation => reservation.Status == ReservationStatus.Confirmed)
            .Where(reservation => reservation.StartUtc < toUtc.UtcDateTime && reservation.EndUtc > fromUtc.UtcDateTime)
            .OrderBy(reservation => reservation.StartUtc)
            .ToArrayAsync(cancellationToken);
    }

    private static Room MapRoom(RoomEntity room)
    {
        return new Room(
            room.Id,
            room.Code,
            room.Name,
            room.Location,
            room.Capacity,
            DeserializeAmenities(room.AmenitiesJson));
    }

    private static Reservation MapReservation(ReservationEntity reservation)
    {
        return new Reservation(
            reservation.Id,
            reservation.RoomId,
            reservation.ReservedBy,
            reservation.Purpose,
            new DateTimeOffset(DateTime.SpecifyKind(reservation.StartUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reservation.EndUtc, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(reservation.CreatedUtc, DateTimeKind.Utc)),
            reservation.Status);
    }

    private static string[] DeserializeAmenities(string amenitiesJson)
    {
        return JsonSerializer.Deserialize<string[]>(amenitiesJson, JsonSerializerOptions) ?? [];
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
