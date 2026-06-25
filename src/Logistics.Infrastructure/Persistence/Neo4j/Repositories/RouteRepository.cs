using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Entities;
using Logistics.Infrastructure.Persistence.Neo4j.Cypher;
using Logistics.Infrastructure.Persistence.Neo4j.Mappings;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

public sealed class RouteRepository(Neo4jContext context) : IRouteRepository
{
    public async Task<bool> WarehouseExistsAsync(string warehouseId, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(RouteCypher.WarehouseExists, new { id = warehouseId });
            return (await cursor.SingleAsync())["exists"].As<bool>();
        });
    }

    public async Task<string> AddAsync(Route route, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Write);
        return await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(RouteCypher.CreateRoute, RouteMapper.ToCreateParameters(route));
            return (await cursor.SingleAsync())["id"].As<string>();
        });
    }

    public async Task<Route?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(RouteCypher.GetById, new { id });
            var records = await cursor.ToListAsync();
            return records.Count == 0 ? null : RouteMapper.ToDomain(RouteDao.FromRecord(records[0]));
        });
    }

    public async Task<IReadOnlyList<Route>> ListAsync(CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(RouteCypher.List);
            var records = await cursor.ToListAsync();
            return (IReadOnlyList<Route>)records
                .Select(r => RouteMapper.ToDomain(RouteDao.FromRecord(r)))
                .ToList();
        });
    }
}
