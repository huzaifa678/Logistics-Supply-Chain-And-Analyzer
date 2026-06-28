using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Messaging;
using Logistics.Application.Shipments.IntegrationEvents;
using Logistics.Domain.Common;
using Logistics.Domain.Events;

namespace Logistics.Application.Shipments.EventHandlers;

/// <summary>
/// Bridges the internal domain event onto the integration-event backbone: when a shipment is
/// delayed, publish a <see cref="ShipmentDelayedIntegrationEvent"/> for other services to consume.
/// Runs on the in-process background worker, so the broker call is off the request thread.
/// </summary>
public sealed class PublishShipmentDelayedHandler(IIntegrationEventPublisher publisher) : IDomainEventHandler
{
    public bool CanHandle(DomainEvent domainEvent) => domainEvent is ShipmentDelayedEvent;

    public Task HandleAsync(DomainEvent domainEvent, CancellationToken ct)
    {
        var e = (ShipmentDelayedEvent)domainEvent;
        return publisher.PublishAsync(
            new ShipmentDelayedIntegrationEvent(e.ShipmentId, e.TrackingNumber, e.Reason, e.CustomerPhone), ct);
    }
}
