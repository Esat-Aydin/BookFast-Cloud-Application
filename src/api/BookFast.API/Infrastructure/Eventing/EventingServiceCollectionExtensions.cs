// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : EventingServiceCollectionExtensions.cs
//  Project         : BookFast.API
// ******************************************************************************

using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookFast.API.Infrastructure.Eventing;

public static class EventingServiceCollectionExtensions
{
    public static IServiceCollection AddBookFastEventing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EventingOptions>(configuration.GetSection(EventingOptions.SectionName));

        EventingOptions options = configuration.GetSection(EventingOptions.SectionName).Get<EventingOptions>() ?? new EventingOptions();

        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IIntegrationEventConsumer, ReportingReservationIntegrationConsumer>());
        services.AddScoped<OutboxDispatcher>();

        if (options.Mode == IntegrationTransportMode.ServiceBus)
        {
            string? connectionString = configuration.GetConnectionString("BookFastServiceBus");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'BookFastServiceBus' is required when Eventing:Mode is 'ServiceBus'.");
            }

            services.AddSingleton(_ => new ServiceBusClient(connectionString));
            services.AddScoped<IIntegrationEventPublisher, ServiceBusIntegrationEventPublisher>();
        }
        else
        {
            services.AddScoped<IIntegrationEventPublisher, InMemoryIntegrationEventPublisher>();
        }

        if (options.EnableBackgroundDispatcher)
        {
            services.AddHostedService<OutboxDispatcherBackgroundService>();
        }

        return services;
    }
}
