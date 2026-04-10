// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : CorrelationIdMiddleware.cs
//  Project         : BookFast.API
// ******************************************************************************

using System.Diagnostics;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BookFast.API.Diagnostics;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private readonly ObservabilityOptions _options;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger, IOptions<ObservabilityOptions> options)
    {
        this._next = next;
        this._logger = logger;
        this._options = options.Value;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        string correlationId = ResolveCorrelationId(httpContext.Request.Headers[this._options.CorrelationHeaderName]);

        httpContext.Items[ApiRequestContext.CorrelationIdItemKey] = correlationId;
        httpContext.Response.Headers[this._options.CorrelationHeaderName] = correlationId;

        Activity? activity = Activity.Current;
        if (activity is not null)
        {
            activity.SetTag("bookfast.correlation_id", correlationId);
            activity.SetTag("bookfast.trace_id", httpContext.TraceIdentifier);
        }

        Dictionary<string, object?> scopeState = new()
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = httpContext.TraceIdentifier,
            ["RequestMethod"] = httpContext.Request.Method,
            ["RequestPath"] = ApiRequestContext.GetRequestPath(httpContext)
        };

        using IDisposable? scope = this._logger.BeginScope(scopeState);
        await this._next(httpContext);
    }

    private static string ResolveCorrelationId(StringValues headerValues)
    {
        string? correlationId = headerValues.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
