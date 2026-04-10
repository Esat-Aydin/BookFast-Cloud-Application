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

using HotChocolate;
using HotChocolate.Types;

namespace BookFast.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public sealed class RoomQueries
{
    [GraphQLDescription("Returns rooms for consumer-driven discovery and selection scenarios.")]
    public async Task<IReadOnlyList<RoomResponse>> GetRooms(
        string? search = null,
        string? location = null,
        int? minimumCapacity = null,
        RoomSortOrder sortBy = RoomSortOrder.CodeAscending,
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

        string? normalizedLocation = Normalize(location);
        if (normalizedLocation is not null)
        {
            filteredRooms = filteredRooms.Where(room =>
                room.Location.Equals(normalizedLocation, StringComparison.OrdinalIgnoreCase));
        }

        if (minimumCapacity is int capacity)
        {
            filteredRooms = filteredRooms.Where(room => room.Capacity >= capacity);
        }

        return [..ApplySorting(filteredRooms, sortBy)
            .Skip(skip)
            .Take(first)
            .Select(ApiContractMapper.MapRoom)];
    }

    [GraphQLDescription("Returns a single room by identifier.")]
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

    [GraphQLDescription("Returns availability for one room within a requested time window.")]
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

    private static IOrderedEnumerable<Room> ApplySorting(
        IEnumerable<Room> rooms,
        RoomSortOrder sortBy)
    {
        return sortBy switch
        {
            RoomSortOrder.CodeDescending => rooms.OrderByDescending(
                room => room.Code,
                StringComparer.OrdinalIgnoreCase),
            RoomSortOrder.NameAscending => rooms.OrderBy(
                room => room.Name,
                StringComparer.OrdinalIgnoreCase),
            RoomSortOrder.NameDescending => rooms.OrderByDescending(
                room => room.Name,
                StringComparer.OrdinalIgnoreCase),
            RoomSortOrder.LocationAscending => rooms.OrderBy(
                room => room.Location,
                StringComparer.OrdinalIgnoreCase),
            RoomSortOrder.LocationDescending => rooms.OrderByDescending(
                room => room.Location,
                StringComparer.OrdinalIgnoreCase),
            RoomSortOrder.CapacityAscending => rooms.OrderBy(room => room.Capacity),
            RoomSortOrder.CapacityDescending => rooms.OrderByDescending(room => room.Capacity),
            _ => rooms.OrderBy(room => room.Code, StringComparer.OrdinalIgnoreCase)
        };
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
