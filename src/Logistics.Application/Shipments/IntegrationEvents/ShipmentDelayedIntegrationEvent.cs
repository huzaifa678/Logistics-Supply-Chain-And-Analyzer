using Logistics.Application.Common.Messaging;

namespace Logistics.Application.Shipments.IntegrationEvents;

/// <summary>Published to the event backbone when a shipment is delayed. A stable external contract.</summary>
public sealed record ShipmentDelayedIntegrationEvent(
    string ShipmentId,
    string TrackingNumber,
    string Reason,
    string CustomerPhone) : IntegrationEvent;
