using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Migrations;

/// <summary>Schema migration: uniqueness constraints and indexes the app relies on.</summary>
public sealed class M0001_InitialSchema : IGraphMigration
{
    public string Id => "0001_initial_schema";

    private static readonly string[] Statements =
    [
        "CREATE CONSTRAINT warehouse_id IF NOT EXISTS FOR (w:Warehouse) REQUIRE w.id IS UNIQUE",
        "CREATE INDEX warehouse_name IF NOT EXISTS FOR (w:Warehouse) ON (w.name)",

        "CREATE CONSTRAINT shipment_id IF NOT EXISTS FOR (s:Shipment) REQUIRE s.id IS UNIQUE",
        "CREATE CONSTRAINT shipment_tracking IF NOT EXISTS FOR (s:Shipment) REQUIRE s.trackingNumber IS UNIQUE",
        "CREATE INDEX shipment_status IF NOT EXISTS FOR (s:Shipment) ON (s.status)",

        "CREATE CONSTRAINT user_id IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE",
        "CREATE CONSTRAINT user_email IF NOT EXISTS FOR (u:User) REQUIRE u.email IS UNIQUE",
        "CREATE CONSTRAINT refresh_token_id IF NOT EXISTS FOR (t:RefreshToken) REQUIRE t.id IS UNIQUE",
        "CREATE INDEX refresh_token_hash IF NOT EXISTS FOR (t:RefreshToken) ON (t.tokenHash)",
    ];

    public async Task UpAsync(IAsyncSession session, CancellationToken ct)
    {
        foreach (var statement in Statements)
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(statement);
                await cursor.ConsumeAsync();
            });
        }
    }
}
