namespace Logistics.Infrastructure.Persistence.Neo4j.Constraints;

/// <summary>
/// Idempotent startup task that creates uniqueness constraints and indexes.
/// Graph query performance depends on these existing before traffic arrives.
/// </summary>
public sealed class GraphConstraintsInitializer(
    Neo4jContext context,
    Neo4jGraphClientProvider graphClient,
    ILogger<GraphConstraintsInitializer> logger) : IHostedService
{
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await context.VerifyConnectivityAsync();

        // Connect the ORM client once, before any request can use a CRUD repository.
        await graphClient.ConnectAsync();

        await using var session = context.Session(AccessMode.Write);

        foreach (var statement in Statements)
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(statement);
            });
        }

        logger.LogInformation("Neo4j constraints and indexes ensured ({Count} statements).", Statements.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
