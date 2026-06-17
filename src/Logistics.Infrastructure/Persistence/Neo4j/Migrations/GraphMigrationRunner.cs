using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Migrations;

/// <summary>
/// Startup migration engine. Applies every <see cref="IGraphMigration"/> not yet recorded, in
/// <see cref="IGraphMigration.Id"/> order, recording each as a (:__Migration {id, appliedAt}).
///
/// Concurrency across instances: the unique constraint on <c>__Migration.id</c> means if two
/// nodes race to apply the same migration, the loser's record-write throws a constraint violation,
/// which we treat as "already applied". Combined with idempotent migration bodies, this is safe
/// without a separate distributed lock (add leader election if you want stricter ordering).
/// </summary>
public sealed class GraphMigrationRunner(
    Neo4jContext context,
    Neo4jGraphClientProvider graphClient,
    IEnumerable<IGraphMigration> migrations,
    ILogger<GraphMigrationRunner> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await context.VerifyConnectivityAsync();

        // Connect the ORM client once, before any request can use a CRUD repository.
        await graphClient.ConnectAsync();

        await using var session = context.Session(AccessMode.Write);

        await EnsureMigrationConstraintAsync(session);
        var applied = await GetAppliedIdsAsync(session);

        var pending = migrations.OrderBy(m => m.Id, StringComparer.Ordinal)
                                .Where(m => !applied.Contains(m.Id))
                                .ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Graph schema up to date ({Count} migrations applied).", applied.Count);
            return;
        }

        foreach (var migration in pending)
        {
            logger.LogInformation("Applying migration {Id}…", migration.Id);
            await migration.UpAsync(session, cancellationToken);

            if (await TryRecordAsync(session, migration.Id))
                logger.LogInformation("Applied migration {Id}.", migration.Id);
            else
                logger.LogInformation("Migration {Id} already recorded by another instance.", migration.Id);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureMigrationConstraintAsync(IAsyncSession session) =>
        await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "CREATE CONSTRAINT migration_id IF NOT EXISTS FOR (m:__Migration) REQUIRE m.id IS UNIQUE");
            await cursor.ConsumeAsync();
        });

    private static async Task<HashSet<string>> GetAppliedIdsAsync(IAsyncSession session) =>
        await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (m:__Migration) RETURN m.id AS id");
            var records = await cursor.ToListAsync();
            return records.Select(r => r["id"].As<string>()).ToHashSet();
        });

    /// <summary>Records the migration; returns false if another instance recorded it first.</summary>
    private static async Task<bool> TryRecordAsync(IAsyncSession session, string id)
    {
        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "CREATE (m:__Migration {id: $id, appliedAt: $appliedAt})",
                    new { id, appliedAt = DateTimeOffset.UtcNow.ToString("o") });
                await cursor.ConsumeAsync();
            });
            return true;
        }
        catch (ClientException ex) when (ex.Code.Contains("ConstraintValidationFailed"))
        {
            return false; // another instance won the race
        }
    }
}
