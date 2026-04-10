// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomAvailabilityOverviewResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Contracts.Rooms;

using HotChocolate;

namespace BookFast.API.Contracts.ReadModels;

[GraphQLDescription("Summarizes room availability across multiple rooms for one requested time window.")]
public sealed record RoomAvailabilityOverviewResponse(
    Guid RoomId,
    string RoomCode,
    string RoomName,
    string Location,
    int Capacity,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    bool IsAvailable,
    int ConflictCount,
    AvailabilityConflictResponse[] Conflicts);
