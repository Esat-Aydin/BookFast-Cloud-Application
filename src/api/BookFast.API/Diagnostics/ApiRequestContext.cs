// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiRequestContext.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public static class ApiRequestContext
{
    public const string CorrelationIdItemKey = "BookFast.CorrelationId";

    public static string GetCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out object? value) &&
            value is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return httpContext.TraceIdentifier;
    }

    public static string GetRequestPath(HttpContext httpContext)
    {
        string? path = httpContext.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        return path;
    }

    public static string GetEndpointDisplayName(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return "unknown";
        }

        string? displayName = endpoint.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "unknown";
        }

        return displayName;
    }
}
