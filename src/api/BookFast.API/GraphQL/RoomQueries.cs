// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomQueries.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;
using BookFast.API.Contracts.Rooms;
using BookFast.API.Domain;
using BookFast.API.Services;

using HotChocolate.Types;

namespace BookFast.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public sealed class RoomQueries
{
    public IReadOnlyList<RoomResponse> GetRooms(
        IBookFastCatalog catalog,
        string? search = null,
        int? minimumCapacity = null,
        int skip = 0,
        int first = 20)
    {
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureMinimumCapacity(minimumCapacity);

        IEnumerable<Room> rooms = catalog.ListRooms();
        string? normalizedSearch = Normalize(search);
        if (normalizedSearch is not null)
        {
            rooms = rooms.Where(room =>
                room.Code.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                room.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                room.Location.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (minimumCapacity is int capacity)
        {
            rooms = rooms.Where(room => room.Capacity >= capacity);
        }

        return [.. rooms.Skip(skip).Take(first).Select(ApiContractMapper.MapRoom)];
    }

    public RoomResponse? GetRoom(Guid id, IBookFastCatalog catalog)
    {
        Room? room = catalog.GetRoom(id);
        if (room is null)
        {
            return null;
        }

        return ApiContractMapper.MapRoom(room);
    }

    public RoomAvailabilityResponse GetRoomAvailability(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IBookFastCatalog catalog)
    {
        GraphQLQueryGuard.EnsureTimeRange(fromUtc, toUtc);

        Room? room = catalog.GetRoom(roomId);
        if (room is null)
        {
            throw GraphQLQueryGuard.CreateError("ROOM_NOT_FOUND", $"No room exists with id '{roomId}'.");
        }

        AvailabilityCheckResult result = catalog.CheckAvailability(roomId, fromUtc, toUtc);
        return ApiContractMapper.MapAvailability(room, fromUtc, toUtc, result);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
