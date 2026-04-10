// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RoomResponse.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Contracts.Rooms;

public sealed record RoomResponse(
    Guid Id,
    string Code,
    string Name,
    string Location,
    int Capacity,
    string[] Amenities);
