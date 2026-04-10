// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReservationCreationStatus.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Services;

public enum ReservationCreationStatus
{
    Created = 1,
    RoomNotFound = 2,
    InvalidTimeRange = 3,
    StartTimeInPast = 4,
    Conflict = 5
}
