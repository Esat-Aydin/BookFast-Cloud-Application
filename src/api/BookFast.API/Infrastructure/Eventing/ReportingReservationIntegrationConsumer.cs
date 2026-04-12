// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingReservationIntegrationConsumer.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Infrastructure.Persistence;

using BookFast.Integration.Contracts;

using Microsoft.EntityFrameworkCore;

namespace BookFast.API.Infrastructure.Eventing;

public sealed class ReportingReservationIntegrationConsumer : IIntegrationEventConsumer
{
    private readonly BookFastDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ReportingReservationIntegrationConsumer(BookFastDbContext dbContext, TimeProvider timeProvider)
    {
        this._dbContext = dbContext;
        this._timeProvider = timeProvider;
    }

    public string ConsumerName => "ReportingReservationSync";

    public bool CanHandle(string eventType)
    {
        return eventType == IntegrationEventNames.ReservationCreated;
    }

    public async Task HandleAsync(OutboxMessageEnvelope message, CancellationToken cancellationToken)
    {
        ReservationCreatedIntegrationEvent integrationEvent =
            IntegrationEventJsonSerializer.Deserialize<ReservationCreatedIntegrationEvent>(message.PayloadJson);

        RoomEntity room = await this._dbContext.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == integrationEvent.RoomId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Reporting consumer could not resolve room '{integrationEvent.RoomId}' for reservation '{integrationEvent.ReservationId}'.");

        ReportingReservationSyncEntity? existingSync = await this._dbContext.ReportingReservationSyncs
            .SingleOrDefaultAsync(sync => sync.ReservationId == integrationEvent.ReservationId, cancellationToken);

        if (existingSync is null)
        {
            existingSync = new ReportingReservationSyncEntity
            {
                ReservationId = integrationEvent.ReservationId
            };

            await this._dbContext.ReportingReservationSyncs.AddAsync(existingSync, cancellationToken);
        }

        existingSync.RoomId = integrationEvent.RoomId;
        existingSync.RoomCode = room.Code;
        existingSync.RoomName = room.Name;
        existingSync.Location = room.Location;
        existingSync.ReservedBy = integrationEvent.ReservedBy;
        existingSync.Purpose = integrationEvent.Purpose;
        existingSync.StartUtc = integrationEvent.StartUtc.UtcDateTime;
        existingSync.EndUtc = integrationEvent.EndUtc.UtcDateTime;
        existingSync.CreatedUtc = integrationEvent.CreatedUtc.UtcDateTime;
        existingSync.Status = integrationEvent.Status;
        existingSync.SourceMessageId = message.MessageId;
        existingSync.CorrelationId = integrationEvent.CorrelationId;
        existingSync.LastSyncedUtc = this._timeProvider.GetUtcNow().UtcDateTime;
    }
}
