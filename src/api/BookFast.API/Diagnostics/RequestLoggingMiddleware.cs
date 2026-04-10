// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : RequestLoggingMiddleware.cs
//  Project         : BookFast.API
// ******************************************************************************

using System.Diagnostics;

namespace BookFast.API.Diagnostics;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        this._next = next;
        this._logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        long startTimestamp = Stopwatch.GetTimestamp();
        string requestPath = ApiRequestContext.GetRequestPath(httpContext);
        string correlationId = ApiRequestContext.GetCorrelationId(httpContext);

        this._logger.LogInformation(
            ApiLogEvents.RequestStarted,
            "API request started. Method: {Method}. Path: {Path}. TraceId: {TraceId}. CorrelationId: {CorrelationId}",
            httpContext.Request.Method,
            requestPath,
            httpContext.TraceIdentifier,
            correlationId);

        await this._next(httpContext);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        int statusCode = httpContext.Response.StatusCode;
        LogLevel completionLevel = GetCompletionLogLevel(statusCode);
        string endpointDisplayName = ApiRequestContext.GetEndpointDisplayName(httpContext);

        this._logger.Log(
            completionLevel,
            ApiLogEvents.RequestCompleted,
            "API request completed. Method: {Method}. Path: {Path}. Endpoint: {Endpoint}. StatusCode: {StatusCode}. ElapsedMs: {ElapsedMs}. TraceId: {TraceId}. CorrelationId: {CorrelationId}",
            httpContext.Request.Method,
            requestPath,
            endpointDisplayName,
            statusCode,
            Math.Round(elapsed.TotalMilliseconds, 2),
            httpContext.TraceIdentifier,
            correlationId);
    }

    private static LogLevel GetCompletionLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= StatusCodes.Status500InternalServerError => LogLevel.Error,
            >= StatusCodes.Status400BadRequest => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
