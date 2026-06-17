using Logistics.Infrastructure.Persistence.Neo4j;
using Logistics.Infrastructure.Persistence.Neo4j.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Verifies the migration runner applies migrations, backfills data, and is idempotent.</summary>
public class GraphMigrationRunnerTests : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder("neo4j:5-community").Build();
    private Neo4jContext _context = null!;
    private Neo4jGraphClientProvider _graph = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _context = new Neo4jContext(Options.Create(new Neo4jSettings
        {
            Uri = _container.GetConnectionString(),
            Username = "neo4j",
            Password = "neo4j"
        }));
        _graph = new Neo4jGraphClientProvider(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    private GraphMigrationRunner NewRunner() => new(
        _context,
        _graph,
        [new M0001_InitialSchema(), new M0002_DefaultWarehouseCapacity()],
        NullLogger<GraphMigrationRunner>.Instance);

    private async Task<int> CountAppliedAsync()
    {
        await using var session = _context.Session(AccessMode.Read);
        return await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (m:__Migration) RETURN count(m) AS c");
            return (await cursor.SingleAsync())["c"].As<int>();
        });
    }

    [Fact]
    public async Task Run_AppliesAllMigrations_BackfillsData_AndIsIdempotent()
    {
        // A warehouse created before capacityUnits existed.
        await using (var seed = _context.Session(AccessMode.Write))
            await seed.RunAsync("CREATE (:Warehouse {id:'w1', name:'Legacy'})");

        // First run applies both migrations.
        await NewRunner().StartAsync(CancellationToken.None);
        Assert.Equal(2, await CountAppliedAsync());

        // Data migration backfilled the missing property.
        await using (var read = _context.Session(AccessMode.Read))
        {
            var capacity = await read.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (w:Warehouse {id:'w1'}) RETURN w.capacityUnits AS cap");
                return (await cursor.SingleAsync())["cap"].As<int>();
            });
            Assert.Equal(0, capacity);
        }

        // Second run is a no-op — no duplicate migration records, no error.
        await NewRunner().StartAsync(CancellationToken.None);
        Assert.Equal(2, await CountAppliedAsync());
    }
}
