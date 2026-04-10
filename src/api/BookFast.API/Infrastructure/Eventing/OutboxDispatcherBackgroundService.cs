// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : OutboxDispatcherBackgroundService.cs
//  Project         : BookFast.API
// ******************************************************************************

using Microsoft.Extensions.Options;

namespace BookFast.API.Infrastructure.Eventing;

public sealed class OutboxDispatcherBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly EventingOptions _options;
    private readonly ILogger<OutboxDispatcherBackgroundService> _logger;

    public OutboxDispatcherBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<EventingOptions> options,
        ILogger<OutboxDispatcherBackgroundService> logger)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._options = options.Value;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan pollingInterval = TimeSpan.FromSeconds(Math.Max(1, this._options.PollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = this._serviceScopeFactory.CreateAsyncScope();
                OutboxDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "Background outbox dispatch failed.");
            }

            try
            {
                await Task.Delay(pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
