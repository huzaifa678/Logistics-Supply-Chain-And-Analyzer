using Logistics.Domain.Common;
using Logistics.Domain.Enums;
using Logistics.Domain.Events;

namespace Logistics.Domain.Entities;

/// <summary>
/// A consignment moving through the supply-chain graph. In Neo4j:
///   (:Shipment)-[:ORIGINATES_AT]->(:Warehouse)
///   (:Shipment)-[:DESTINED_FOR]->(:Warehouse)
///   (:Shipment)-[:CARRIES]->(:Product)
/// </summary>
public sealed class Shipment : BaseEntity, IAggregateRoot
{
    public string TrackingNumber { get; private set; }
    public string OriginWarehouseId { get; private set; }
    public string DestinationWarehouseId { get; private set; }
    public double WeightKg { get; private set; }
    public TransportMode Mode { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EstimatedArrival { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private Shipment(
        string trackingNumber, string originWarehouseId, string destinationWarehouseId,
        double weightKg, TransportMode mode)
    {
        TrackingNumber = trackingNumber;
        OriginWarehouseId = originWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        WeightKg = weightKg;
        Mode = mode;
        Status = ShipmentStatus.Created;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Shipment Create(
        string trackingNumber, string originWarehouseId, string destinationWarehouseId,
        double weightKg, TransportMode mode)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber)) throw new ArgumentException("Tracking number required.", nameof(trackingNumber));
        if (string.IsNullOrWhiteSpace(originWarehouseId)) throw new ArgumentException("Origin required.", nameof(originWarehouseId));
        if (string.IsNullOrWhiteSpace(destinationWarehouseId)) throw new ArgumentException("Destination required.", nameof(destinationWarehouseId));
        if (originWarehouseId == destinationWarehouseId) throw new ArgumentException("Origin and destination must differ.");
        if (weightKg <= 0) throw new ArgumentOutOfRangeException(nameof(weightKg));

        return new Shipment(trackingNumber, originWarehouseId, destinationWarehouseId, weightKg, mode);
    }

    public void Dispatch(DateTimeOffset estimatedArrival)
    {
        EnsureStatus(ShipmentStatus.Created);
        Status = ShipmentStatus.InTransit;
        EstimatedArrival = estimatedArrival;
    }

    public void MarkDelayed(string reason)
    {
        EnsureStatus(ShipmentStatus.InTransit);
        Status = ShipmentStatus.Delayed;
        RaiseEvent(new ShipmentDelayedEvent(Id, TrackingNumber, reason));
    }

    public void Deliver()
    {
        if (Status is not (ShipmentStatus.InTransit or ShipmentStatus.Delayed))
            throw new InvalidOperationException($"Cannot deliver a shipment in status {Status}.");
        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is ShipmentStatus.Delivered)
            throw new InvalidOperationException("Delivered shipments cannot be cancelled.");
        Status = ShipmentStatus.Cancelled;
    }

    private void EnsureStatus(ShipmentStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected status {expected} but was {Status}.");
    }

    public static Shipment Rehydrate(
        string id, string trackingNumber, string originWarehouseId, string destinationWarehouseId,
        double weightKg, TransportMode mode, ShipmentStatus status,
        DateTimeOffset createdAt, DateTimeOffset? estimatedArrival, DateTimeOffset? deliveredAt)
        => new(trackingNumber, originWarehouseId, destinationWarehouseId, weightKg, mode)
        {
            Id = id,
            Status = status,
            CreatedAt = createdAt,
            EstimatedArrival = estimatedArrival,
            DeliveredAt = deliveredAt
        };
}
