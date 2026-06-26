using Logistics.Domain.Common;

namespace Logistics.Domain.Events;

public sealed record ShipmentCreatedEvent(string ShipmentId, string TrackingNumber) : DomainEvent;
