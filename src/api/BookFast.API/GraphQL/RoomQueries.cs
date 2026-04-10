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
    public async Task<IReadOnlyList<RoomResponse>> GetRooms(
        string? search = null,
        int? minimumCapacity = null,
        int skip = 0,
        int first = 20,
        IBookFastCatalog catalog = default!,
        CancellationToken cancellationToken = default)
    {
        GraphQLQueryGuard.EnsurePagingArguments(skip, first);
        GraphQLQueryGuard.EnsureMinimumCapacity(minimumCapacity);

        IReadOnlyCollection<Room> rooms = await catalog.ListRoomsAsync(cancellationToken);
        IEnumerable<Room> filteredRooms = rooms;
        string? normalizedSearch = Normalize(search);
        if (normalizedSearch is not null)
        {
            filteredRooms = filteredRooms.Where(room =>
                room.Code.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                room.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                room.Location.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        if (minimumCapacity is int capacity)
        {
            filteredRooms = filteredRooms.Where(room => room.Capacity >= capacity);
        }

        return [..filteredRooms.Skip(skip).Take(first).Select(ApiContractMapper.MapRoom)];
    }

    public async Task<RoomResponse?> GetRoom(
        Guid id,
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        Room? room = await catalog.GetRoomAsync(id, cancellationToken);
        if (room is null)
        {
            return null;
        }

        return ApiContractMapper.MapRoom(room);
    }

    public async Task<RoomAvailabilityResponse> GetRoomAvailability(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IBookFastCatalog catalog,
        CancellationToken cancellationToken)
    {
        GraphQLQueryGuard.EnsureTimeRange(fromUtc, toUtc);

        Room? room = await catalog.GetRoomAsync(roomId, cancellationToken);
        if (room is null)
        {
            throw GraphQLQueryGuard.CreateError("ROOM_NOT_FOUND", $"No room exists with id '{roomId}'.");
        }

        AvailabilityCheckResult result = await catalog.CheckAvailabilityAsync(
            roomId,
            fromUtc,
            toUtc,
            cancellationToken);

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
