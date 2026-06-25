using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Entities;
using Logistics.Infrastructure.Persistence.Neo4j.Cypher;
using Logistics.Infrastructure.Persistence.Neo4j.Mappings;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

public sealed class WarehouseRepository(Neo4jContext context) : IWarehouseRepository
{
    public async Task<string> AddAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Write);
        return await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(WarehouseCypher.Create, WarehouseMapper.ToCreateParameters(warehouse));
            return (await cursor.SingleAsync())["id"].As<string>();
        });
    }

    public async Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(WarehouseCypher.List);
            var records = await cursor.ToListAsync();
            return (IReadOnlyList<Warehouse>)records
                .Select(r => WarehouseMapper.ToDomain(WarehouseDao.FromRecord(r)))
                .ToList();
        });
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(WarehouseCypher.Exists, new { id });
            return (await cursor.SingleAsync())["exists"].As<bool>();
        });
    }
}
