// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiLogEvents.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public static class ApiLogEvents
{
    public static readonly EventId RequestStarted = new(1000, nameof(RequestStarted));

    public static readonly EventId RequestCompleted = new(1001, nameof(RequestCompleted));

    public static readonly EventId ValidationFailed = new(2000, nameof(ValidationFailed));

    public static readonly EventId ConflictDetected = new(2001, nameof(ConflictDetected));

    public static readonly EventId FailureDetected = new(3000, nameof(FailureDetected));

    public static readonly EventId UnhandledFailure = new(5000, nameof(UnhandledFailure));
}
