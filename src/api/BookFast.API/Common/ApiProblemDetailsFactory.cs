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
    public static ProblemDetails Create(int statusCode, string title, string detail, string instance)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance
        };
    }
}
