// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomEndpoints.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.Rooms;
using BookFast.API.Diagnostics;
using BookFast.API.Domain;
using BookFast.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookFast.API.Endpoints;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomEndpoints(this RouteGroupBuilder apiGroup)
    {
        RouteGroupBuilder roomsGroup = apiGroup
            .MapGroup("/rooms")
            .WithTags("Rooms");

        roomsGroup.MapGet("/", GetRooms)
            .WithName("ListRooms")
            .Produces<RoomResponse[]>(StatusCodes.Status200OK);

        roomsGroup.MapGet("/{roomId:guid}", GetRoomById)
            .WithName("GetRoomById")
            .Produces<RoomResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        roomsGroup.MapGet("/{roomId:guid}/availability", GetAvailability)
            .WithName("GetRoomAvailability")
            .Produces<RoomAvailabilityResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        return apiGroup;
    }

    private static IResult GetRooms(IBookFastCatalog catalog)
    {
        IReadOnlyCollection<Room> rooms = catalog.ListRooms();
        RoomResponse[] response = [..rooms.Select(ApiContractMapper.MapRoom)];

        return Results.Ok(response);
    }

    private static IResult GetRoomById(Guid roomId, IBookFastCatalog catalog)
    {
        Room? room = catalog.GetRoom(roomId);
        if (room is null)
        {
            ProblemDetails problem = CreateRoomNotFoundProblem(roomId);
            return Results.NotFound(problem);
        }

        return Results.Ok(ApiContractMapper.MapRoom(room));
    }

    private static IResult GetAvailability(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IBookFastCatalog catalog,
        HttpContext httpContext,
        ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("RoomEndpoints");
        Dictionary<string, string[]> errors = ValidateAvailabilityQuery(fromUtc, toUtc);
        if (errors.Count > 0)
        {
            ApiRequestLog.LogValidationFailure(logger, httpContext, errors);
            return CreateValidationProblemResult(
                httpContext,
                errors,
                "One or more availability query parameters are invalid.",
                ApiErrorCodes.InvalidAvailabilityQuery);
        }

        AvailabilityCheckResult result = catalog.CheckAvailability(roomId, fromUtc, toUtc);
        if (!result.RoomExists)
        {
            ProblemDetails problem = CreateRoomNotFoundProblem(roomId);
            return CreateProblemResult(problem);
        }

        if (!result.TimeRangeValid)
        {
            Dictionary<string, string[]> timeRangeError = CreateTimeRangeError();
            ApiRequestLog.LogValidationFailure(logger, httpContext, timeRangeError);
            return CreateValidationProblemResult(
                httpContext,
                timeRangeError,
                "The requested room availability window is invalid.",
                ApiErrorCodes.InvalidAvailabilityQuery);
        }

        Room? room = catalog.GetRoom(roomId);
        if (room is null)
        {
            ProblemDetails problem = CreateRoomNotFoundProblem(roomId);
            return CreateProblemResult(problem);
        }

        RoomAvailabilityResponse response = ApiContractMapper.MapAvailability(room, fromUtc, toUtc, result);

        if (!result.IsAvailable)
        {
            ApiRequestLog.LogConflict(
                logger,
                httpContext,
                "RoomAvailability",
                roomId.ToString(),
                response.Conflicts.Length,
                "The requested room has one or more overlapping confirmed reservations.");
        }

        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> ValidateAvailabilityQuery(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        Dictionary<string, string[]> errors = [];

        if (fromUtc == default)
        {
            errors["fromUtc"] = ["fromUtc query parameter is required."];
        }

        if (toUtc == default)
        {
            errors["toUtc"] = ["toUtc query parameter is required."];
        }

        if (fromUtc != default && toUtc != default && fromUtc >= toUtc)
        {
            errors["timeRange"] = ["fromUtc must be earlier than toUtc."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> CreateTimeRangeError()
    {
        return new Dictionary<string, string[]>
        {
            ["timeRange"] = ["The requested time range is invalid."]
        };
    }

    private static ProblemDetails CreateRoomNotFoundProblem(Guid roomId)
    {
        return ApiProblemDetailsFactory.Create(
            StatusCodes.Status404NotFound,
            "Room not found",
            $"No room exists with id '{roomId}'.",
            $"/api/v1/rooms/{roomId}",
            ApiErrorCodes.RoomNotFound);
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

}
