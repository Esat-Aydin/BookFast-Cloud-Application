// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomAvailabilityResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Rooms;

public sealed record RoomAvailabilityResponse(
    Guid RoomId,
    string RoomCode,
    string RoomName,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    bool IsAvailable,
    AvailabilityConflictResponse[] Conflicts);
