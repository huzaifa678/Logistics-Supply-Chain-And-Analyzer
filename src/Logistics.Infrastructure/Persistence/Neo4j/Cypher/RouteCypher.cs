namespace Logistics.Infrastructure.Persistence.Neo4j.Cypher;

/// <summary>
/// All Cypher for route persistence lives here so query strings never leak into
/// the Application or Api layers.
/// </summary>
internal static class RouteCypher
{
    public const string WarehouseExists = """
        MATCH (w:Warehouse {id: $id})
        RETURN count(w) > 0 AS exists
        """;

    public const string CreateRoute = """
        MATCH (o:Warehouse {id: $originId}), (d:Warehouse {id: $destinationId})
        CREATE (o)-[r:CONNECTS_TO {
            id: $id,
            distanceKm: $distanceKm,
            cost: $cost,
            mode: $mode
        }]->(d)
        RETURN r.id AS id
        """;

    public const string GetById = """
        MATCH (o:Warehouse)-[r:CONNECTS_TO {id: $id}]->(d:Warehouse)
        RETURN r.id AS id, o.id AS originId, d.id AS destinationId,
               r.distanceKm AS distanceKm, r.cost AS cost, r.mode AS mode
        """;

    public const string List = """
        MATCH (o:Warehouse)-[r:CONNECTS_TO]->(d:Warehouse)
        RETURN r.id AS id, o.id AS originId, d.id AS destinationId,
               r.distanceKm AS distanceKm, r.cost AS cost, r.mode AS mode
        """;

    // Weighted shortest path. Swap for a GDS call (gds.shortestPath.dijkstra)
    // on large graphs; this variable-length form is fine for modest datasets.
    public const string ShortestPath = """
        MATCH (o:Warehouse {id: $originId}), (d:Warehouse {id: $destinationId})
        MATCH p = shortestPath((o)-[:CONNECTS_TO*..15]->(d))
        RETURN [n IN nodes(p) | n.id] AS warehouseIds,
               reduce(total = 0.0, r IN relationships(p) | total + r.distanceKm) AS totalDistanceKm,
               length(p) AS hops
        """;
}
