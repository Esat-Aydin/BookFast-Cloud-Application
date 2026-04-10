// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
// 
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ObservabilityOptions.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Diagnostics;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string CorrelationHeaderName { get; init; } = "X-Correlation-Id";
}
