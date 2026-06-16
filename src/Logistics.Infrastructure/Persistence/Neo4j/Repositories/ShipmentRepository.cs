using System.Runtime.CompilerServices;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

public sealed class ShipmentRepository(Neo4jContext context) : IShipmentRepository
{
    public async Task<bool> WarehouseExistsAsync(string warehouseId, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(ShipmentCypher.WarehouseExists, new { id = warehouseId });
            return (await cursor.SingleAsync())["exists"].As<bool>();
        });
    }

    public async Task<string> AddAsync(Shipment shipment, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Write);
        return await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(ShipmentCypher.Create, ShipmentMapper.ToCreateParameters(shipment));
            return (await cursor.SingleAsync())["id"].As<string>();
        });
    }

    public async Task UpdateAsync(Shipment shipment, CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Write);
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(ShipmentCypher.Update, ShipmentMapper.ToUpdateParameters(shipment));
            await cursor.ConsumeAsync();
        });
    }

    public Task<Shipment?> GetByIdAsync(string id, CancellationToken ct = default)
        => QuerySingle(ShipmentCypher.GetById, new { id });

    public Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default)
        => QuerySingle(ShipmentCypher.GetByTrackingNumber, new { trackingNumber });

    public async IAsyncEnumerable<Shipment> StreamByStatusAsync(
        ShipmentStatus status, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var session = context.Session(AccessMode.Read);
        await using var tx = await session.BeginTransactionAsync();

        var cursor = await tx.RunAsync(ShipmentCypher.GetByStatus, new { status = status.ToString() });

        // FetchAsync advances one record at a time — the full result is never buffered.
        while (await cursor.FetchAsync())
        {
            ct.ThrowIfCancellationRequested();
            yield return ShipmentMapper.ToDomain(ShipmentDao.FromRecord(cursor.Current));
        }

        await tx.CommitAsync();
    }

    private async Task<Shipment?> QuerySingle(string cypher, object parameters)
    {
        await using var session = context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(cypher, parameters);
            var records = await cursor.ToListAsync();
            return records.Count == 0 ? null : ShipmentMapper.ToDomain(ShipmentDao.FromRecord(records[0]));
        });
    }
}
