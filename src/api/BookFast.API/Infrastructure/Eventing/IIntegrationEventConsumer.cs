// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IIntegrationEventConsumer.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public interface IIntegrationEventConsumer
{
    string ConsumerName { get; }

    bool CanHandle(string eventType);

    Task HandleAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken);
}
