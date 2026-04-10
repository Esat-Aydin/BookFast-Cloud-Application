// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiCorsOptions.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public sealed class ApiCorsOptions
{
    public const string PolicyName = "BookFastConfiguredOrigins";
    public const string SectionName = "ApiCors";

    public string[] AllowedOrigins { get; init; } = [];

    public string[] AllowedMethods { get; init; } =
    [
        "GET",
        "POST",
        "OPTIONS"
    ];

    public string[] AllowedHeaders { get; init; } =
    [
        "Authorization",
        "Content-Type",
        "X-Correlation-Id"
    ];

    public string[] ExposedHeaders { get; init; } =
    [
        "X-Correlation-Id",
        "api-selected-version",
        "api-supported-versions"
    ];
}
