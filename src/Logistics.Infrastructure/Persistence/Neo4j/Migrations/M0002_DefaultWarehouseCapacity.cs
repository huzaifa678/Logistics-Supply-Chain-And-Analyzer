using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Migrations;

/// <summary>
/// Data migration: backfills a default <c>capacityUnits</c> on any pre-existing Warehouse that
/// is missing the property (e.g. nodes created before the field existed). Idempotent — the
/// <c>WHERE w.capacityUnits IS NULL</c> guard means re-running touches nothing.
/// </summary>
public sealed class M0002_DefaultWarehouseCapacity : IGraphMigration
{
    public string Id => "0002_default_warehouse_capacity";

    private const string Backfill = """
        MATCH (w:Warehouse)
        WHERE w.capacityUnits IS NULL
        SET w.capacityUnits = 0
        """;

    public async Task UpAsync(IAsyncSession session, CancellationToken ct)
    {
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(Backfill);
            await cursor.ConsumeAsync();
        });
    }
}
