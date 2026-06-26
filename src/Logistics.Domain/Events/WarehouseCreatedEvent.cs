using Logistics.Domain.Common;

namespace Logistics.Domain.Events;

public sealed record WarehouseCreatedEvent(string WarehouseId, string Name) : DomainEvent;
