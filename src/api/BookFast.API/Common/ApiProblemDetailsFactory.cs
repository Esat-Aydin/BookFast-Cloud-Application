// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiProblemDetailsFactory.cs
//  Project         : BookFast.API
// ******************************************************************************

using Microsoft.AspNetCore.Mvc;

namespace BookFast.API.Common;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(int statusCode, string title, string detail, string instance, string errorCode)
    {
        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance
        };

        problemDetails.Extensions["errorCode"] = errorCode;

        return problemDetails;
    }

    public static ValidationProblemDetails CreateValidationProblem(
        Dictionary<string, string[]> errors,
        string detail,
        string instance,
        string errorCode)
    {
        ValidationProblemDetails validationProblemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed",
            Detail = detail,
            Instance = instance
        };

        validationProblemDetails.Extensions["errorCode"] = errorCode;

        return validationProblemDetails;
    }

    public static string ResolveDefaultErrorCode(int? statusCode)
    {
        if (statusCode == StatusCodes.Status400BadRequest)
        {
            return ApiErrorCodes.RequestValidationFailed;
        }

        if (statusCode == StatusCodes.Status404NotFound)
        {
            return ApiErrorCodes.RoomNotFound;
        }

        if (statusCode == StatusCodes.Status409Conflict)
        {
            return ApiErrorCodes.ReservationConflict;
        }

        return ApiErrorCodes.UnexpectedServerError;
    }
}
