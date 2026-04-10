// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : BookFastIntegrationEventFactory.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Contracts.Integration;
using BookFast.API.Domain;

namespace BookFast.API.Infrastructure.Eventing;

public static class BookFastIntegrationEventFactory
{
    public static IReadOnlyCollection<OutboxMessageEntity> CreateReservationCreatedMessages(
        Reservation reservation,
        DateTimeOffset occurredUtc,
        string? correlationId)
    {
        Guid reservationCreatedMessageId = Guid.NewGuid();
        Guid availabilityChangedMessageId = Guid.NewGuid();
        string? normalizedCorrelationId = Normalize(correlationId);

        ReservationCreatedIntegrationEvent reservationCreated = new(
            reservationCreatedMessageId,
            occurredUtc,
            reservation.Id,
            reservation.RoomId,
            reservation.ReservedBy,
            reservation.Purpose,
            reservation.StartUtc,
            reservation.EndUtc,
            reservation.CreatedUtc,
            reservation.Status.ToString(),
            normalizedCorrelationId);

        RoomAvailabilityChangedIntegrationEvent availabilityChanged = new(
            availabilityChangedMessageId,
            occurredUtc,
            reservation.RoomId,
            reservation.Id,
            reservation.StartUtc,
            reservation.EndUtc,
            "ReservationCreated",
            normalizedCorrelationId);

        return
        [
            CreateMessage(
                reservationCreatedMessageId,
                IntegrationEventNames.ReservationCreated,
                "Reservation",
                reservation.Id,
                occurredUtc,
                normalizedCorrelationId,
                reservationCreated),
            CreateMessage(
                availabilityChangedMessageId,
                IntegrationEventNames.RoomAvailabilityChanged,
                "Room",
                reservation.RoomId,
                occurredUtc,
                normalizedCorrelationId,
                availabilityChanged)
        ];
    }

    private static OutboxMessageEntity CreateMessage(
        Guid messageId,
        string eventType,
        string aggregateType,
        Guid aggregateId,
        DateTimeOffset occurredUtc,
        string? correlationId,
        object payload)
    {
        return new OutboxMessageEntity
        {
            Id = messageId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            OccurredUtc = occurredUtc.UtcDateTime,
            PayloadJson = IntegrationEventJsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            Status = OutboxMessageStatus.Pending,
            DeliveryAttemptCount = 0,
            NextAttemptUtc = occurredUtc.UtcDateTime
        };
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
