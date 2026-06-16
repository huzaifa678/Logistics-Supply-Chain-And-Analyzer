using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Common;
using Logistics.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Logistics.Application.Shipments.EventHandlers;

/// <summary>
/// Reacts to a shipment being delayed — here it just logs, but this is where you'd notify
/// customers, raise an alert, or recompute an ETA. Runs on the background worker, not the
/// request thread, so slow side effects never block the API response.
/// </summary>
public sealed class ShipmentDelayedEventHandler(ILogger<ShipmentDelayedEventHandler> logger)
    : IDomainEventHandler
{
    public bool CanHandle(DomainEvent domainEvent) => domainEvent is ShipmentDelayedEvent;

    public Task HandleAsync(DomainEvent domainEvent, CancellationToken ct)
    {
        var e = (ShipmentDelayedEvent)domainEvent;
        logger.LogWarning(
            "Shipment {TrackingNumber} ({ShipmentId}) delayed: {Reason}",
            e.TrackingNumber, e.ShipmentId, e.Reason);
        return Task.CompletedTask;
    }
}
