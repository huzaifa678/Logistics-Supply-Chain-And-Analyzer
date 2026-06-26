using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Common;
using Logistics.Domain.Events;

namespace Logistics.Application.Common.Webhooks;

/// <summary>
/// Fans selected domain events out to an external subscriber as webhooks. Runs on the in-process
/// background worker (off the request thread). Add a new event mapping here without touching the
/// dispatcher. Delivery resilience (retry/circuit breaker) lives in the IWebhookSender adapter.
/// </summary>
public sealed class WebhookDispatchHandler(IWebhookSender sender) : IDomainEventHandler
{
    public bool CanHandle(DomainEvent domainEvent) =>
        domainEvent is ShipmentCreatedEvent or ShipmentDelayedEvent or WarehouseCreatedEvent;

    public Task HandleAsync(DomainEvent domainEvent, CancellationToken ct) => domainEvent switch
    {
        ShipmentCreatedEvent e => sender.SendAsync(
            new WebhookEvent("shipment.created", new { e.ShipmentId, e.TrackingNumber }), ct),
        ShipmentDelayedEvent e => sender.SendAsync(
            new WebhookEvent("shipment.delayed", new { e.ShipmentId, e.TrackingNumber, e.Reason }), ct),
        WarehouseCreatedEvent e => sender.SendAsync(
            new WebhookEvent("warehouse.created", new { e.WarehouseId, e.Name }), ct),
        _ => Task.CompletedTask,
    };
}
