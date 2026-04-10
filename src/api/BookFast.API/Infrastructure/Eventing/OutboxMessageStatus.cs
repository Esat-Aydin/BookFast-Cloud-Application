// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxMessageStatus.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public enum OutboxMessageStatus
{
    Pending = 1,
    Published = 2,
    DeadLettered = 3,
    Processing = 4
}
