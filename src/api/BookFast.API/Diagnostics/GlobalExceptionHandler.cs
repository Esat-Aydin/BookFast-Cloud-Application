// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : GlobalExceptionHandler.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Common;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookFast.API.Diagnostics;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        this._logger = logger;
        this._problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string correlationId = ApiRequestContext.GetCorrelationId(httpContext);

        this._logger.LogError(
            ApiLogEvents.UnhandledFailure,
            exception,
            "Unhandled API exception. Method: {Method}. Path: {Path}. TraceId: {TraceId}. CorrelationId: {CorrelationId}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? "/",
            httpContext.TraceIdentifier,
            correlationId);

        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            StatusCodes.Status500InternalServerError,
            "Unexpected server error",
            "An unexpected error occurred while processing the request.",
            ApiRequestContext.GetRequestPath(httpContext),
            ApiErrorCodes.UnexpectedServerError);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await this._problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
