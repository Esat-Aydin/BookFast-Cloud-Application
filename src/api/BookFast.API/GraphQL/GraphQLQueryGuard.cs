// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : GraphQLQueryGuard.cs
//  Project         : BookFast.API
// ******************************************************************************

using HotChocolate;

namespace BookFast.API.GraphQL;

public static class GraphQLQueryGuard
{
    private const int MaxPageSize = 50;

    public static void EnsurePagingArguments(int skip, int first)
    {
        if (skip < 0)
        {
            throw CreateError("PAGING_ARGUMENT_OUT_OF_RANGE", "skip must be zero or greater.");
        }

        if (first < 1 || first > MaxPageSize)
        {
            throw CreateError(
                "PAGING_ARGUMENT_OUT_OF_RANGE",
                $"first must be between 1 and {MaxPageSize}.");
        }
    }

    public static void EnsureMinimumCapacity(int? minimumCapacity)
    {
        if (minimumCapacity is < 1)
        {
            throw CreateError("FILTER_ARGUMENT_INVALID", "minimumCapacity must be greater than zero.");
        }
    }

    public static void EnsureTimeRange(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        if (fromUtc == default || toUtc == default || fromUtc >= toUtc)
        {
            throw CreateError("TIME_RANGE_INVALID", "fromUtc must be earlier than toUtc.");
        }
    }

    public static void EnsureOptionalTimeRange(DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value >= toUtc.Value)
        {
            throw CreateError("TIME_RANGE_INVALID", "fromUtc must be earlier than toUtc.");
        }
    }

    public static GraphQLException CreateError(string code, string message)
    {
        return new GraphQLException(
            ErrorBuilder.New()
                .SetCode(code)
                .SetMessage(message)
                .Build());
    }
}
