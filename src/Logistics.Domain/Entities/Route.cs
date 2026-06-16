using Logistics.Domain.Common;
using Logistics.Domain.Enums;

namespace Logistics.Domain.Entities;

/// <summary>
/// A directed connection between two warehouses. In Neo4j this is the
/// (:Warehouse)-[:CONNECTS_TO]->(:Warehouse) relationship, weighted by distance/cost.
/// </summary>
public sealed class Route : BaseEntity, IAggregateRoot
{
    public string OriginId { get; private set; }
    public string DestinationId { get; private set; }
    public double DistanceKm { get; private set; }
    public decimal Cost { get; private set; }
    public TransportMode Mode { get; private set; }

    private Route(string originId, string destinationId, double distanceKm, decimal cost, TransportMode mode)
    {
        OriginId = originId;
        DestinationId = destinationId;
        DistanceKm = distanceKm;
        Cost = cost;
        Mode = mode;
    }

    public static Route Create(string originId, string destinationId, double distanceKm, decimal cost, TransportMode mode)
    {
        if (string.IsNullOrWhiteSpace(originId)) throw new ArgumentException("Origin required.", nameof(originId));
        if (string.IsNullOrWhiteSpace(destinationId)) throw new ArgumentException("Destination required.", nameof(destinationId));
        if (originId == destinationId) throw new ArgumentException("A route cannot start and end at the same warehouse.");
        if (distanceKm <= 0) throw new ArgumentOutOfRangeException(nameof(distanceKm));

        return new Route(originId, destinationId, distanceKm, cost, mode);
    }

    public static Route Rehydrate(string id, string originId, string destinationId, double distanceKm, decimal cost, TransportMode mode)
        => new(originId, destinationId, distanceKm, cost, mode) { Id = id };
}
