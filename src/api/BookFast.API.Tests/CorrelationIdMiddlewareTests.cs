// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : CorrelationIdMiddlewareTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookFast.API.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldGenerateCorrelationId_WhenRequestHeaderIsMissing()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        CorrelationIdMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.TryGetValue("X-Correlation-Id", out Microsoft.Extensions.Primitives.StringValues headerValues));

        string correlationId = headerValues.ToString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(correlationId, ApiRequestContext.GetCorrelationId(httpContext));
    }

    [Fact]
    public async Task InvokeAsync_ShouldPreserveIncomingCorrelationId_WhenRequestHeaderExists()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "candidate-123";
        CorrelationIdMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal("candidate-123", httpContext.Response.Headers["X-Correlation-Id"].ToString());
        Assert.Equal("candidate-123", ApiRequestContext.GetCorrelationId(httpContext));
    }

    private static CorrelationIdMiddleware CreateMiddleware()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        NullLogger<CorrelationIdMiddleware> logger = NullLogger<CorrelationIdMiddleware>.Instance;
        ObservabilityOptions options = new ObservabilityOptions
        {
            CorrelationHeaderName = "X-Correlation-Id"
        };

        return new CorrelationIdMiddleware(next, logger, Options.Create(options));
    }
}
