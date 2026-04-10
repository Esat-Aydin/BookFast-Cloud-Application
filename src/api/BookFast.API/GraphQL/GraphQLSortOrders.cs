// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : GraphQLSortOrders.cs
//  Project         : BookFast.API
// ******************************************************************************

using HotChocolate;

namespace BookFast.API.GraphQL;

[GraphQLDescription("Supported sort orders for room discovery queries.")]
public enum RoomSortOrder
{
    CodeAscending = 1,
    CodeDescending = 2,
    NameAscending = 3,
    NameDescending = 4,
    LocationAscending = 5,
    LocationDescending = 6,
    CapacityAscending = 7,
    CapacityDescending = 8
}

[GraphQLDescription("Supported sort orders for reservation discovery queries.")]
public enum ReservationSortOrder
{
    StartUtcAscending = 1,
    StartUtcDescending = 2,
    EndUtcAscending = 3,
    EndUtcDescending = 4,
    CreatedUtcAscending = 5,
    CreatedUtcDescending = 6,
    ReservedByAscending = 7,
    ReservedByDescending = 8
}

[GraphQLDescription("Supported sort orders for room availability overview queries.")]
public enum AvailabilityOverviewSortOrder
{
    RoomCodeAscending = 1,
    RoomCodeDescending = 2,
    LocationAscending = 3,
    LocationDescending = 4,
    CapacityAscending = 5,
    CapacityDescending = 6,
    AvailabilityAscending = 7,
    AvailabilityDescending = 8
}

[GraphQLDescription("Supported sort orders for location occupancy overview queries.")]
public enum OccupancyOverviewSortOrder
{
    LocationAscending = 1,
    LocationDescending = 2,
    RoomOccupancyRateAscending = 3,
    RoomOccupancyRateDescending = 4,
    CapacityOccupancyRateAscending = 5,
    CapacityOccupancyRateDescending = 6,
    ReservedRoomsAscending = 7,
    ReservedRoomsDescending = 8
}
