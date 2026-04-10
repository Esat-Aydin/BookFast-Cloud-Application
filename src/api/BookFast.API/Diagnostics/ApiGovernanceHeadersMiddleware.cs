// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiGovernanceHeadersMiddleware.cs
//  Project         : BookFast.API
// ******************************************************************************

using Microsoft.Extensions.Options;

namespace BookFast.API.Diagnostics;

public sealed class ApiGovernanceHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiGovernanceOptions _options;

    public ApiGovernanceHeadersMiddleware(RequestDelegate next, IOptions<ApiGovernanceOptions> options)
    {
        this._next = next;
        this._options = options.Value;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (ShouldApply(httpContext.Request.Path))
        {
            httpContext.Response.Headers["api-selected-version"] = this._options.CurrentVersion;
            httpContext.Response.Headers["api-supported-versions"] = string.Join(", ", this._options.SupportedVersions);
        }

        await this._next(httpContext);
    }

    private bool ShouldApply(PathString requestPath)
    {
        return requestPath.StartsWithSegments(this._options.VersionedApiBasePath, StringComparison.OrdinalIgnoreCase);
    }
}
