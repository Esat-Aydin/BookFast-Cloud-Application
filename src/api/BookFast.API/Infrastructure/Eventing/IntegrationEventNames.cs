// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IntegrationEventNames.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public static class IntegrationEventNames
{
    public const string ReservationCreated = "reservation.created.v1";

    public const string RoomAvailabilityChanged = "room.availability.changed.v1";
}
