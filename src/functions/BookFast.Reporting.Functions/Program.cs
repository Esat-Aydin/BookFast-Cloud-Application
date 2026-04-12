// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : Program.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

using BookFast.Reporting.Functions.Persistence;
using BookFast.Reporting.Functions.Processing;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        string? connectionString = context.Configuration.GetConnectionString("BookFastDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'BookFastDatabase' is required.");
        }

        services.AddDbContext<ReportingDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure());
        });

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<ReportingReservationMessageProcessor>();
    })
    .Build();

await host.RunAsync();
