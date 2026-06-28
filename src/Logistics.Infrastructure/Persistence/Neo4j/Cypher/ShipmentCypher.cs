namespace Logistics.Infrastructure.Persistence.Neo4j.Cypher;

/// <summary>
/// Cypher for the shipment graph model:
///   (s:Shipment)-[:ORIGINATES_AT]->(o:Warehouse)
///   (s:Shipment)-[:DESTINED_FOR]->(d:Warehouse)
/// Datetimes are stored as ISO-8601 strings for round-trip stability.
/// </summary>
internal static class ShipmentCypher
{
    public const string WarehouseExists = """
        MATCH (w:Warehouse {id: $id}) RETURN count(w) > 0 AS exists
        """;

    public const string Create = """
        MATCH (o:Warehouse {id: $originId}), (d:Warehouse {id: $destinationId})
        CREATE (s:Shipment {
            id: $id, trackingNumber: $trackingNumber, customerPhone: $customerPhone,
            weightKg: $weightKg, mode: $mode, status: $status, createdAt: $createdAt,
            estimatedArrival: $estimatedArrival, deliveredAt: $deliveredAt
        })
        CREATE (s)-[:ORIGINATES_AT]->(o)
        CREATE (s)-[:DESTINED_FOR]->(d)
        RETURN s.id AS id
        """;

    public const string Update = """
        MATCH (s:Shipment {id: $id})
        SET s.status = $status,
            s.estimatedArrival = $estimatedArrival,
            s.deliveredAt = $deliveredAt
        """;

    private const string Projection = """
        RETURN s.id AS id, s.trackingNumber AS trackingNumber,
               o.id AS originId, d.id AS destinationId, s.customerPhone AS customerPhone,
               s.weightKg AS weightKg, s.mode AS mode, s.status AS status,
               s.createdAt AS createdAt, s.estimatedArrival AS estimatedArrival,
               s.deliveredAt AS deliveredAt
        """;

    public const string GetById = """
        MATCH (s:Shipment {id: $id})-[:ORIGINATES_AT]->(o:Warehouse)
        MATCH (s)-[:DESTINED_FOR]->(d:Warehouse)
        """ + Projection;

    public const string GetByTrackingNumber = """
        MATCH (s:Shipment {trackingNumber: $trackingNumber})-[:ORIGINATES_AT]->(o:Warehouse)
        MATCH (s)-[:DESTINED_FOR]->(d:Warehouse)
        """ + Projection;

    // Active shipments (not yet delivered/cancelled) touching a warehouse — congestion signal.
    public const string ActiveShipmentCount = """
        MATCH (s:Shipment)-[:ORIGINATES_AT|DESTINED_FOR]->(w:Warehouse {id: $id})
        WHERE s.status IN ['Created', 'InTransit', 'Delayed']
        RETURN count(DISTINCT s) AS activeCount
        """;

    // Streamed read — potentially large result set, consumed lazily (never fully buffered).
    public const string GetByStatus = """
        MATCH (s:Shipment {status: $status})-[:ORIGINATES_AT]->(o:Warehouse)
        MATCH (s)-[:DESTINED_FOR]->(d:Warehouse)
        """ + Projection + """

        ORDER BY s.createdAt DESC
        """;
}
