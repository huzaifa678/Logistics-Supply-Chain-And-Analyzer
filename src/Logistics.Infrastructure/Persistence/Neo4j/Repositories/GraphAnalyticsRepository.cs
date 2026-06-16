using Logistics.Application.Common.Interfaces;
using Logistics.Infrastructure.Persistence.Neo4j.Cypher;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

public sealed class GraphAnalyticsRepository(Neo4jContext context) : IGraphAnalyticsRepository
{
    public async Task<ShortestPathResult?> GetShortestPathAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(RouteCypher.ShortestPath, new
            {
                originId = originWarehouseId,
                destinationId = destinationWarehouseId
            });

            var records = await cursor.ToListAsync();
            if (records.Count == 0) return null;

            var r = records[0];
            return new ShortestPathResult(
                r["warehouseIds"].As<List<string>>(),
                r["totalDistanceKm"].As<double>(),
                r["hops"].As<int>());
        });
    }

    public async Task<int> GetActiveShipmentCountAsync(string warehouseId, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(ShipmentCypher.ActiveShipmentCount, new { id = warehouseId });
            return (await cursor.SingleAsync())["activeCount"].As<int>();
        });
    }
}
