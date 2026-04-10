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

    private static IResult GetReservations(IBookFastCatalog catalog)
    {
        IReadOnlyCollection<Reservation> reservations = catalog.ListReservations();
        ReservationResponse[] response = [..reservations
            .Select(reservation => ApiContractMapper.MapReservation(reservation, catalog))
            .Where(reservation => reservation is not null)
            .Select(reservation => reservation!)];

        return Results.Ok(response);
    }

    private static IResult GetReservationById(Guid reservationId, IBookFastCatalog catalog)
    {
        Reservation? reservation = catalog.GetReservation(reservationId);
        if (reservation is null)
        {
            ProblemDetails problem = CreateReservationNotFoundProblem(reservationId);
            return Results.NotFound(problem);
        }

        ReservationResponse? response = ApiContractMapper.MapReservation(reservation, catalog);
        if (response is null)
        {
            return Results.Problem(
                title: "Reservation data is inconsistent",
                detail: "The reservation exists, but the associated room could not be resolved.",
                instance: $"/api/v1/reservations/{reservationId}",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(response);
    }

    private static IResult CreateReservation(
        CreateReservationRequest request,
        IBookFastCatalog catalog,
        ILoggerFactory loggerFactory,
        HttpContext httpContext)
    {
        ILogger logger = loggerFactory.CreateLogger("ReservationEndpoints");
        Dictionary<string, string[]> errors = ValidateCreateReservationRequest(request);
        if (errors.Count > 0)
        {
            ApiRequestLog.LogValidationFailure(logger, httpContext, errors);
            return Results.ValidationProblem(errors);
        }

        ReservationCreationResult result = catalog.CreateReservation(
            request.RoomId,
            request.ReservedBy,
            request.Purpose,
            request.StartUtc,
            request.EndUtc);

        if (result.Status == ReservationCreationStatus.RoomNotFound)
        {
            logger.LogWarning("Reservation creation rejected because room {RoomId} does not exist.", request.RoomId);
            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                StatusCodes.Status404NotFound,
                "Room not found",
                $"No room exists with id '{request.RoomId}'.",
                "/api/v1/reservations");

            return Results.NotFound(problem);
        }

        if (result.Status == ReservationCreationStatus.InvalidTimeRange)
        {
            Dictionary<string, string[]> timeRangeError = CreateTimeRangeError();
            ApiRequestLog.LogValidationFailure(logger, httpContext, timeRangeError);
            return Results.ValidationProblem(timeRangeError);
        }

        if (result.Status == ReservationCreationStatus.StartTimeInPast)
        {
            Dictionary<string, string[]> startTimeError = new()
            {
                ["startUtc"] = ["startUtc must be in the future."]
            };

            ApiRequestLog.LogValidationFailure(logger, httpContext, startTimeError);
            return Results.ValidationProblem(startTimeError);
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
                "/api/v1/reservations");

            return Results.Conflict(problem);
        }

        Reservation? reservation = result.Reservation;
        if (reservation is null)
        {
            ApiRequestLog.LogFailure(
                logger,
                httpContext,
                "ReservationCreateMissingEntity",
                "Reservation creation returned no reservation instance.");

            return Results.Problem(
                title: "Reservation creation failed",
                detail: "The reservation could not be created.",
                instance: "/api/v1/reservations",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        ReservationResponse? response = ApiContractMapper.MapReservation(reservation, catalog);
        if (response is null)
        {
            ApiRequestLog.LogFailure(
                logger,
                httpContext,
                "ReservationCreateMissingRoom",
                "Reservation was created but the associated room could not be resolved.");

            return Results.Problem(
                title: "Reservation creation failed",
                detail: "The reservation was created, but the associated room could not be resolved.",
                instance: "/api/v1/reservations",
                statusCode: StatusCodes.Status500InternalServerError);
        }

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
            $"/api/v1/reservations/{reservationId}");
    }

}
