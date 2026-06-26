using Logistics.Domain.Common;
using Logistics.Domain.Events;
using Logistics.Domain.ValueObjects;

namespace Logistics.Domain.Entities;

/// <summary>
/// A node in the supply-chain graph. Connected to other warehouses via <see cref="Route"/>
/// relationships (modeled as :CONNECTS_TO edges in Neo4j).
/// </summary>
public sealed class Warehouse : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public GeoLocation Location { get; private set; }
    public int CapacityUnits { get; private set; }

    private Warehouse(string name, GeoLocation location, int capacityUnits)
    {
        Name = name;
        Location = location;
        CapacityUnits = capacityUnits;
    }

    public static Warehouse Create(string name, GeoLocation location, int capacityUnits)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Warehouse name is required.", nameof(name));
        if (capacityUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(capacityUnits));

        var warehouse = new Warehouse(name, location, capacityUnits);
        warehouse.RaiseEvent(new WarehouseCreatedEvent(warehouse.Id, warehouse.Name));
        return warehouse;
    }

    /// <summary>Rehydrate from persistence without re-running creation invariants.</summary>
    public static Warehouse Rehydrate(string id, string name, GeoLocation location, int capacityUnits)
        => new(name, location, capacityUnits) { Id = id };
}
