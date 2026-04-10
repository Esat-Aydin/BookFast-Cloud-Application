// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : BookFastDbContextFactory.cs
//  Project         : BookFast.API
// ******************************************************************************

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookFast.API.Infrastructure.Persistence;

public sealed class BookFastDbContextFactory : IDesignTimeDbContextFactory<BookFastDbContext>
{
    public BookFastDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("BOOKFAST_SQL_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=BookFast;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        DbContextOptionsBuilder<BookFastDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure();
            });

        return new BookFastDbContext(optionsBuilder.Options);
    }
}
