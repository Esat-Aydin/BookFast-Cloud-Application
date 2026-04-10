// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiGovernanceHeadersMiddlewareTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BookFast.API.Tests;

public sealed class ApiGovernanceHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldWriteVersionHeaders_ForVersionedApiRequests()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/rooms";
        ApiGovernanceHeadersMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal("1.0", httpContext.Response.Headers["api-selected-version"].ToString());
        Assert.Equal("1.0", httpContext.Response.Headers["api-supported-versions"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipVersionHeaders_ForNonVersionedRequests()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/health";
        ApiGovernanceHeadersMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        Assert.False(httpContext.Response.Headers.ContainsKey("api-selected-version"));
        Assert.False(httpContext.Response.Headers.ContainsKey("api-supported-versions"));
    }

    private static ApiGovernanceHeadersMiddleware CreateMiddleware()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        ApiGovernanceOptions options = new ApiGovernanceOptions
        {
            CurrentVersion = "1.0",
            SupportedVersions = ["1.0"],
            VersionedApiBasePath = "/api/v1"
        };

        return new ApiGovernanceHeadersMiddleware(next, Options.Create(options));
    }
}
