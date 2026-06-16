namespace Logistics.Infrastructure.Persistence.Neo4j.Mappings;

/// <summary>
/// DAO mirroring the persisted shape of a Shipment node (+ its origin/destination ids).
/// This is the boundary type between the driver and the domain — the domain never
/// references Neo4j, and the driver never references the domain entity.
/// </summary>
internal sealed record ShipmentDao(
    string Id,
    string TrackingNumber,
    string OriginId,
    string DestinationId,
    double WeightKg,
    string Mode,
    string Status,
    string CreatedAt,
    string? EstimatedArrival,
    string? DeliveredAt)
{
    public static ShipmentDao FromRecord(IRecord r) => new(
        r["id"].As<string>(),
        r["trackingNumber"].As<string>(),
        r["originId"].As<string>(),
        r["destinationId"].As<string>(),
        r["weightKg"].As<double>(),
        r["mode"].As<string>(),
        r["status"].As<string>(),
        r["createdAt"].As<string>(),
        r["estimatedArrival"].As<string?>(),
        r["deliveredAt"].As<string?>());
}

/// <summary>Single Responsibility: converts between the Shipment domain entity and persistence.</summary>
internal static class ShipmentMapper
{
    public static Shipment ToDomain(ShipmentDao dao) => Shipment.Rehydrate(
        dao.Id,
        dao.TrackingNumber,
        dao.OriginId,
        dao.DestinationId,
        dao.WeightKg,
        Enum.Parse<TransportMode>(dao.Mode),
        Enum.Parse<ShipmentStatus>(dao.Status),
        DateTimeOffset.Parse(dao.CreatedAt),
        dao.EstimatedArrival is null ? null : DateTimeOffset.Parse(dao.EstimatedArrival),
        dao.DeliveredAt is null ? null : DateTimeOffset.Parse(dao.DeliveredAt));

    /// <summary>Parameters for the create statement (includes the warehouse ids it links to).</summary>
    public static object ToCreateParameters(Shipment s) => new
    {
        id = s.Id,
        trackingNumber = s.TrackingNumber,
        originId = s.OriginWarehouseId,
        destinationId = s.DestinationWarehouseId,
        weightKg = s.WeightKg,
        mode = s.Mode.ToString(),
        status = s.Status.ToString(),
        createdAt = Neo4jValue.Iso(s.CreatedAt),
        estimatedArrival = Neo4jValue.IsoOrNull(s.EstimatedArrival),
        deliveredAt = Neo4jValue.IsoOrNull(s.DeliveredAt)
    };

    public static object ToUpdateParameters(Shipment s) => new
    {
        id = s.Id,
        status = s.Status.ToString(),
        estimatedArrival = Neo4jValue.IsoOrNull(s.EstimatedArrival),
        deliveredAt = Neo4jValue.IsoOrNull(s.DeliveredAt)
    };
}
