// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : LocationOccupancySummaryResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

using HotChocolate;

namespace BookFast.API.Contracts.ReadModels;

[GraphQLDescription("Summarizes location occupancy for a requested time window.")]
public sealed record LocationOccupancySummaryResponse(
    string Location,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int TotalRooms,
    int ReservedRooms,
    int AvailableRooms,
    int TotalCapacity,
    int ReservedCapacity,
    int ActiveReservations,
    double RoomOccupancyRate,
    double CapacityOccupancyRate);
