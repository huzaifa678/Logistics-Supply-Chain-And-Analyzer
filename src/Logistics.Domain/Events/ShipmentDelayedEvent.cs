using Logistics.Domain.Common;

namespace Logistics.Domain.Events;

public sealed record ShipmentDelayedEvent(string ShipmentId, string TrackingNumber, string Reason) : DomainEvent;
