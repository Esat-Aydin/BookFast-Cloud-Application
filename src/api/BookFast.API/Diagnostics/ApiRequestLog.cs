// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiRequestLog.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public static class ApiRequestLog
{
    public static void LogValidationFailure(ILogger logger, HttpContext httpContext, IReadOnlyDictionary<string, string[]> errors)
    {
        string errorSummary = CreateErrorSummary(errors);

        logger.LogWarning(
            ApiLogEvents.ValidationFailed,
            "API validation failed. Method: {Method}. Path: {Path}. Endpoint: {Endpoint}. TraceId: {TraceId}. CorrelationId: {CorrelationId}. ErrorCount: {ErrorCount}. Errors: {Errors}",
            httpContext.Request.Method,
            ApiRequestContext.GetRequestPath(httpContext),
            ApiRequestContext.GetEndpointDisplayName(httpContext),
            httpContext.TraceIdentifier,
            ApiRequestContext.GetCorrelationId(httpContext),
            errors.Count,
            errorSummary);
    }

    public static void LogConflict(
        ILogger logger,
        HttpContext httpContext,
        string conflictType,
        string resourceId,
        int conflictCount,
        string detail)
    {
        logger.LogWarning(
            ApiLogEvents.ConflictDetected,
            "API conflict detected. ConflictType: {ConflictType}. ResourceId: {ResourceId}. Method: {Method}. Path: {Path}. Endpoint: {Endpoint}. TraceId: {TraceId}. CorrelationId: {CorrelationId}. ConflictCount: {ConflictCount}. Detail: {Detail}",
            conflictType,
            resourceId,
            httpContext.Request.Method,
            ApiRequestContext.GetRequestPath(httpContext),
            ApiRequestContext.GetEndpointDisplayName(httpContext),
            httpContext.TraceIdentifier,
            ApiRequestContext.GetCorrelationId(httpContext),
            conflictCount,
            detail);
    }

    public static void LogFailure(
        ILogger logger,
        HttpContext httpContext,
        string failureType,
        string detail)
    {
        logger.LogError(
            ApiLogEvents.FailureDetected,
            "API failure detected. FailureType: {FailureType}. Method: {Method}. Path: {Path}. Endpoint: {Endpoint}. TraceId: {TraceId}. CorrelationId: {CorrelationId}. Detail: {Detail}",
            failureType,
            httpContext.Request.Method,
            ApiRequestContext.GetRequestPath(httpContext),
            ApiRequestContext.GetEndpointDisplayName(httpContext),
            httpContext.TraceIdentifier,
            ApiRequestContext.GetCorrelationId(httpContext),
            detail);
    }

    private static string CreateErrorSummary(IReadOnlyDictionary<string, string[]> errors)
    {
        string[] entries = [..errors.Select(error => $"{error.Key}={string.Join(", ", error.Value)}")];

        return string.Join(" | ", entries);
    }
}
