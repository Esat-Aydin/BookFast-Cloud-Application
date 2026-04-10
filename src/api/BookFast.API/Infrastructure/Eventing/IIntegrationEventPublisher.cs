// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : IIntegrationEventPublisher.cs
//  Project         : BookFast.API
// ******************************************************************************

namespace BookFast.API.Infrastructure.Eventing;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken);
}
