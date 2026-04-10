// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : BookFastDatabaseOptions.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Persistence;

public sealed class BookFastDatabaseOptions
{
    public const string SectionName = "Persistence";
    public const string ConnectionStringName = "BookFastDatabase";

    public bool ApplyMigrationsOnStartup { get; init; }
}
