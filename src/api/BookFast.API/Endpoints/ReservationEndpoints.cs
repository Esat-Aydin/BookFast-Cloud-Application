// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationEndpoints.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.Reservations;
using BookFast.API.Diagnostics;
using BookFast.API.Domain;
using BookFast.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookFast.API.Endpoints;

public static class ReservationEndpoints
{
    public static RouteGroupBuilder MapReservationEndpoints(this RouteGroupBuilder apiGroup)
    {
        RouteGroupBuilder reservationsGroup = apiGroup
            .MapGroup("/reservations")
            .WithTags("Reservations");

        reservationsGroup.MapGet("/", GetReservations)
            .WithName("ListReservations")
            .Produces<ReservationResponse[]>(StatusCodes.Status200OK);

        reservationsGroup.MapGet("/{reservationId:guid}", GetReservationById)
            .WithName("GetReservationById")
            .Produces<ReservationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        reservationsGroup.MapPost("/", CreateReservation)
            .WithName("CreateReservation")
            .Accepts<CreateReservationRequest>("application/json")
            .Produces<ReservationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        return apiGroup;
    }

    private static async Task<IResult> GetReservations(
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Reservation> reservations = await catalog.ListReservationsAsync(cancellationToken);
        Guid[] roomIds = [..reservations
            .Select(reservation => reservation.RoomId)
            .Distinct()];
        IReadOnlyDictionary<Guid, Room> roomsById = await catalog.ListRoomsByIdsAsync(roomIds, cancellationToken);
        ReservationResponse[] response = [..reservations
            .Select(reservation => TryMapReservation(reservation, roomsById))
            .Where(reservation => reservation is not null)
            .Select(reservation => reservation!)];

        return Results.Ok(response);
    }

    private static async Task<IResult> GetReservationById(
        Guid reservationId,
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        Reservation? reservation = await catalog.GetReservationAsync(reservationId, cancellationToken);
        if (reservation is null)
        {
            ProblemDetails problem = CreateReservationNotFoundProblem(reservationId);
            return CreateProblemResult(problem);
        }

        Room? room = await catalog.GetRoomAsync(reservation.RoomId, cancellationToken);
        if (room is null)
        {
            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status500InternalServerError,
                "Reservation data is inconsistent",
                "The reservation exists, but the associated room could not be resolved.",
                $"/api/v1/reservations/{reservationId}",
                ApiErrorCodes.ReservationDataInconsistent);

            return CreateProblemResult(problem);
        }

        ReservationResponse response = ApiContractMapper.MapReservation(reservation, room);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateReservation(
        CreateReservationRequest request,
        IBookFastCatalog catalog,
        ILoggerFactory loggerFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger("ReservationEndpoints");
        Dictionary<string, string[]> errors = ValidateCreateReservationRequest(request);
        if (errors.Count > 0)
        {
            ApiRequestLog.LogValidationFailure(logger, httpContext, errors);
            return CreateValidationProblemResult(
                httpContext,
                errors,
                "One or more reservation request fields are invalid.",
                ApiErrorCodes.InvalidReservationRequest);
        }

        ReservationCreationResult result = await catalog.CreateReservationAsync(
            request.RoomId,
            request.ReservedBy,
            request.Purpose,
            request.StartUtc,
            request.EndUtc,
            cancellationToken);

        if (result.Status == ReservationCreationStatus.RoomNotFound)
        {
            logger.LogWarning("Reservation creation rejected because room {RoomId} does not exist.", request.RoomId);
            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status404NotFound,
                "Room not found",
                $"No room exists with id '{request.RoomId}'.",
                "/api/v1/reservations",
                ApiErrorCodes.RoomNotFound);

            return CreateProblemResult(problem);
        }

        if (result.Status == ReservationCreationStatus.InvalidTimeRange)
        {
            Dictionary<string, string[]> timeRangeError = CreateTimeRangeError();
            ApiRequestLog.LogValidationFailure(logger, httpContext, timeRangeError);
            return CreateValidationProblemResult(
                httpContext,
                timeRangeError,
                "The requested reservation time range is invalid.",
                ApiErrorCodes.InvalidReservationTimeRange);
        }

        if (result.Status == ReservationCreationStatus.StartTimeInPast)
        {
            Dictionary<string, string[]> startTimeError = new()
            {
                ["startUtc"] = ["startUtc must be in the future."]
            };

            ApiRequestLog.LogValidationFailure(logger, httpContext, startTimeError);
            return CreateValidationProblemResult(
                httpContext,
                startTimeError,
                "The reservation start time must be in the future.",
                ApiErrorCodes.ReservationStartTimeInPast);
        }

        if (result.Status == ReservationCreationStatus.Conflict)
        {
            ApiRequestLog.LogConflict(
                logger,
                httpContext,
                "ReservationCreate",
                request.RoomId.ToString(),
                result.ConflictingReservations.Count,
                "The requested reservation overlaps with one or more existing reservations.");

            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status409Conflict,
                "Reservation conflict",
                "The selected room is not available in the requested time window.",
                "/api/v1/reservations",
                ApiErrorCodes.ReservationConflict);

            return CreateProblemResult(problem);
        }

        Reservation? reservation = result.Reservation;
        if (reservation is null)
        {
            ApiRequestLog.LogFailure(
                logger,
                httpContext,
                "ReservationCreateMissingEntity",
                "Reservation creation returned no reservation instance.");

            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status500InternalServerError,
                "Reservation creation failed",
                "The reservation could not be created.",
                "/api/v1/reservations",
                ApiErrorCodes.ReservationCreationFailed);

            return CreateProblemResult(problem);
        }

        Room? room = await catalog.GetRoomAsync(reservation.RoomId, cancellationToken);
        if (room is null)
        {
            ApiRequestLog.LogFailure(
                logger,
                httpContext,
                "ReservationCreateMissingRoom",
                "Reservation was created but the associated room could not be resolved.");

            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status500InternalServerError,
                "Reservation creation failed",
                "The reservation was created, but the associated room could not be resolved.",
                "/api/v1/reservations",
                ApiErrorCodes.ReservationRoomResolutionFailed);

            return CreateProblemResult(problem);
        }

        ReservationResponse response = ApiContractMapper.MapReservation(reservation, room);
        logger.LogInformation(
            "Reservation {ReservationId} created for room {RoomId} between {StartUtc} and {EndUtc}.",
            reservation.Id,
            reservation.RoomId,
            reservation.StartUtc,
            reservation.EndUtc);

        return Results.Created($"/api/v1/reservations/{reservation.Id}", response);
    }

    private static Dictionary<string, string[]> ValidateCreateReservationRequest(CreateReservationRequest request)
    {
        Dictionary<string, string[]> errors = [];

        if (request.RoomId == Guid.Empty)
        {
            errors["roomId"] = ["roomId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ReservedBy))
        {
            errors["reservedBy"] = ["reservedBy is required."];
        }

        if (request.StartUtc == default)
        {
            errors["startUtc"] = ["startUtc is required."];
        }

        if (request.EndUtc == default)
        {
            errors["endUtc"] = ["endUtc is required."];
        }

        if (request.StartUtc != default && request.EndUtc != default && request.StartUtc >= request.EndUtc)
        {
            errors["timeRange"] = ["startUtc must be earlier than endUtc."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> CreateTimeRangeError()
    {
        return new Dictionary<string, string[]>
        {
            ["timeRange"] = ["The requested reservation time range is invalid."]
        };
    }

    private static ProblemDetails CreateReservationNotFoundProblem(Guid reservationId)
    {
        return ApiProblemDetailsFactory.Create(
            StatusCodes.Status404NotFound,
            "Reservation not found",
            $"No reservation exists with id '{reservationId}'.",
            $"/api/v1/reservations/{reservationId}",
            ApiErrorCodes.ReservationNotFound);
    }

    private static IResult CreateProblemResult(ProblemDetails problemDetails)
    {
        return Results.Json(
            problemDetails,
            statusCode: problemDetails.Status,
            contentType: "application/problem+json");
    }

    private static IResult CreateValidationProblemResult(
        HttpContext httpContext,
        Dictionary<string, string[]> errors,
        string detail,
        string errorCode)
    {
        ValidationProblemDetails problemDetails = ApiProblemDetailsFactory.CreateValidationProblem(
            errors,
            detail,
            ApiRequestContext.GetRequestPath(httpContext),
            errorCode);

        return CreateProblemResult(problemDetails);
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
