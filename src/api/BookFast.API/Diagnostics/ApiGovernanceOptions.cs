// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ApiGovernanceOptions.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public sealed class ApiGovernanceOptions
{
    public const string SectionName = "ApiGovernance";

    public string CurrentVersion { get; init; } = "1.0";

    public string[] SupportedVersions { get; init; } = ["1.0"];

    public string VersionedApiBasePath { get; init; } = "/api/v1";
}
