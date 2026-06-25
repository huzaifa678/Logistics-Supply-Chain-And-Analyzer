namespace Logistics.Infrastructure.Persistence.Neo4j.Cypher;

/// <summary>All Cypher for warehouse persistence (kept out of the Application/Api layers).</summary>
internal static class WarehouseCypher
{
    public const string Create = """
        CREATE (w:Warehouse {
            id: $id,
            name: $name,
            latitude: $latitude,
            longitude: $longitude,
            capacityUnits: $capacityUnits
        })
        RETURN w.id AS id
        """;

    public const string List = """
        MATCH (w:Warehouse)
        RETURN w.id AS id, w.name AS name,
               w.latitude AS latitude, w.longitude AS longitude,
               w.capacityUnits AS capacityUnits
        ORDER BY w.name
        """;

    public const string Exists = """
        MATCH (w:Warehouse {id: $id})
        RETURN count(w) > 0 AS exists
        """;
}
