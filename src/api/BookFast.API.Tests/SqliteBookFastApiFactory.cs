// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : SqliteBookFastApiFactory.cs
//  Project         : BookFast.API.Tests
// ******************************************************************************

using BookFast.API.Infrastructure.Eventing;
using BookFast.API.Infrastructure.Persistence;
using BookFast.API.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookFast.API.Tests;

public sealed class SqliteBookFastApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public SqliteBookFastApiFactory()
    {
        this._connection = new SqliteConnection("DataSource=:memory:");
        this._connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
        configurationBuilder.AddInMemoryCollection(

        [
            new KeyValuePair<string, string?>("Persistence:ApplyMigrationsOnStartup", bool.FalseString),
            new KeyValuePair<string, string?>("Eventing:Mode", IntegrationTransportMode.InMemory.ToString()),
            new KeyValuePair<string, string?>("Eventing:EnableBackgroundDispatcher", bool.FalseString)
        ]);
    });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BookFastDbContext>>();
            services.RemoveAll<BookFastDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<BookFastDbContext>>();
            services.RemoveAll<IBookFastCatalog>();

            services.AddDbContext<BookFastDbContext>(options =>
            {
                options.UseSqlite(this._connection);
    });
            services.AddScoped<IBookFastCatalog, SqlBookFastCatalog>();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            using IServiceScope scope = serviceProvider.CreateScope();
            BookFastDbContext dbContext = scope.ServiceProvider.GetRequiredService<BookFastDbContext>();
dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }

    public override async ValueTask DisposeAsync()
{
    await this._connection.DisposeAsync();
    await base.DisposeAsync();
}
}
