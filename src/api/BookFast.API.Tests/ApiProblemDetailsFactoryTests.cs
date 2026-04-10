// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiProblemDetailsFactoryTests.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Common;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookFast.API.Tests;

public sealed class ApiProblemDetailsFactoryTests
{
    [Fact]
    public void Create_ShouldIncludeErrorCode()
    {
        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            StatusCodes.Status404NotFound,
            "Room not found",
            "No room exists with the supplied id.",
            "/api/v1/rooms/candidate",
            ApiErrorCodes.RoomNotFound);

        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal(ApiErrorCodes.RoomNotFound, problemDetails.Extensions["errorCode"]);
    }

    [Fact]
    public void CreateValidationProblem_ShouldIncludeErrorCodeAndErrors()
    {
        Dictionary<string, string[]> errors = new Dictionary<string, string[]>
        {
            ["fromUtc"] = ["fromUtc query parameter is required."]
        };

        ValidationProblemDetails problemDetails = ApiProblemDetailsFactory.CreateValidationProblem(
            errors,
            "One or more request values are invalid.",
            "/api/v1/rooms/candidate/availability",
            ApiErrorCodes.InvalidAvailabilityQuery);

        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Request validation failed", problemDetails.Title);
        Assert.Equal(ApiErrorCodes.InvalidAvailabilityQuery, problemDetails.Extensions["errorCode"]);
        Assert.Equal("fromUtc query parameter is required.", problemDetails.Errors["fromUtc"].Single());
    }
}
