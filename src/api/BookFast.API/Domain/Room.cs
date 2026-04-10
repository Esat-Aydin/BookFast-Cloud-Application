// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Room.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Domain;

public sealed record Room(
    Guid Id,
    string Code,
    string Name,
    string Location,
    int Capacity,
    string[] Amenities);
