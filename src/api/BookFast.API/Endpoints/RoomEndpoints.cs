// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomEndpoints.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.Rooms;
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
        RoomResponse[] response = rooms
            .Select(MapRoom)
            .ToArray();

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

        return Results.Ok(MapRoom(room));
    }

    private static IResult GetAvailability(Guid roomId, DateTimeOffset fromUtc, DateTimeOffset toUtc, IBookFastCatalog catalog)
    {
        Dictionary<string, string[]> errors = ValidateAvailabilityQuery(fromUtc, toUtc);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        AvailabilityCheckResult result = catalog.CheckAvailability(roomId, fromUtc, toUtc);
        if (!result.RoomExists)
        {
            ProblemDetails problem = CreateRoomNotFoundProblem(roomId);
            return Results.NotFound(problem);
        }

        if (!result.TimeRangeValid)
        {
            return Results.ValidationProblem(CreateTimeRangeError());
        }

        Room? room = catalog.GetRoom(roomId);
        if (room is null)
        {
            ProblemDetails problem = CreateRoomNotFoundProblem(roomId);
            return Results.NotFound(problem);
        }

        AvailabilityConflictResponse[] conflicts = result.ConflictingReservations
            .Select(MapConflict)
            .ToArray();

        RoomAvailabilityResponse response = new(
            room.Id,
            room.Code,
            room.Name,
            fromUtc,
            toUtc,
            result.IsAvailable,
            conflicts);

        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> ValidateAvailabilityQuery(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        Dictionary<string, string[]> errors = new Dictionary<string, string[]>();

        if (fromUtc == default)
        {
            errors["fromUtc"] = new[] { "fromUtc query parameter is required." };
        }

        if (toUtc == default)
        {
            errors["toUtc"] = new[] { "toUtc query parameter is required." };
        }

        if (fromUtc != default && toUtc != default && fromUtc >= toUtc)
        {
            errors["timeRange"] = new[] { "fromUtc must be earlier than toUtc." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> CreateTimeRangeError()
    {
        return new Dictionary<string, string[]>
        {
            ["timeRange"] = new[] { "The requested time range is invalid." }
        };
    }

    private static ProblemDetails CreateRoomNotFoundProblem(Guid roomId)
    {
        return ApiProblemDetailsFactory.Create(
            StatusCodes.Status404NotFound,
            "Room not found",
            $"No room exists with id '{roomId}'.",
            $"/api/v1/rooms/{roomId}");
    }

    private static RoomResponse MapRoom(Room room)
    {
        return new RoomResponse(
            room.Id,
            room.Code,
            room.Name,
            room.Location,
            room.Capacity,
            room.Amenities);
    }

    private static AvailabilityConflictResponse MapConflict(Reservation reservation)
    {
        return new AvailabilityConflictResponse(
            reservation.Id,
            reservation.ReservedBy,
            reservation.StartUtc,
            reservation.EndUtc,
            reservation.Status.ToString());
    }
}
