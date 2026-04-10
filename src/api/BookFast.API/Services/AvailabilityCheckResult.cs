// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : AvailabilityCheckResult.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

namespace BookFast.API.Services;

public sealed class AvailabilityCheckResult
{
    private AvailabilityCheckResult(
        bool roomExists,
        bool timeRangeValid,
        bool isAvailable,
        IReadOnlyCollection<Reservation> conflictingReservations)
    {
        this.RoomExists = roomExists;
        this.TimeRangeValid = timeRangeValid;
        this.IsAvailable = isAvailable;
        this.ConflictingReservations = conflictingReservations;
    }

    public bool RoomExists { get; }

    public bool TimeRangeValid { get; }

    public bool IsAvailable { get; }

    public IReadOnlyCollection<Reservation> ConflictingReservations { get; }

    public static AvailabilityCheckResult RoomNotFound()
    {
        return new AvailabilityCheckResult(false, true, false, []);
    }

    public static AvailabilityCheckResult InvalidTimeRange()
    {
        return new AvailabilityCheckResult(true, false, false, []);
    }

    public static AvailabilityCheckResult Available()
    {
        return new AvailabilityCheckResult(true, true, true, []);
    }

    public static AvailabilityCheckResult Unavailable(IReadOnlyCollection<Reservation> conflictingReservations)
    {
        return new AvailabilityCheckResult(true, true, false, conflictingReservations);
    }
}
