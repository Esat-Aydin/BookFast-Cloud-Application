// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiErrorCodes.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Common;

public static class ApiErrorCodes
{
    public const string InvalidAvailabilityQuery = "INVALID_AVAILABILITY_QUERY";
    public const string InvalidReservationRequest = "INVALID_RESERVATION_REQUEST";
    public const string InvalidReservationTimeRange = "INVALID_RESERVATION_TIME_RANGE";
    public const string ReservationStartTimeInPast = "RESERVATION_START_TIME_IN_PAST";
    public const string ReservationConflict = "RESERVATION_CONFLICT";
    public const string ReservationCreationFailed = "RESERVATION_CREATION_FAILED";
    public const string ReservationDataInconsistent = "RESERVATION_DATA_INCONSISTENT";
    public const string ReservationRoomResolutionFailed = "RESERVATION_ROOM_RESOLUTION_FAILED";
    public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
    public const string RoomNotFound = "ROOM_NOT_FOUND";
    public const string RequestValidationFailed = "REQUEST_VALIDATION_FAILED";
    public const string UnexpectedServerError = "UNEXPECTED_SERVER_ERROR";
}
