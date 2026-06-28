using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Logistics.Infrastructure.Persistence.Neo4j;
using Logistics.Infrastructure.Persistence.Neo4j.Repositories;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Verifies the lazily-streamed read path (FetchAsync) against real Neo4j.</summary>
public class ShipmentStreamingTests : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder("neo4j:5-community").Build();
    private Neo4jContext _context = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _context = new Neo4jContext(Options.Create(new Neo4jSettings
        {
            Uri = _container.GetConnectionString(),
            Username = "neo4j",
            Password = "neo4j"
        }));

        await using var session = _context.Session(AccessMode.Write);
        await session.RunAsync("CREATE (:Warehouse {id:'w1', name:'A'}), (:Warehouse {id:'w2', name:'B'})");

        var repo = new ShipmentRepository(_context);
        for (var i = 0; i < 5; i++)
        {
            var s = Shipment.Create($"TRK-{i}", "w1", "w2", "+15551230003", 10 + i, TransportMode.Road);
            await repo.AddAsync(s);
        }
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task StreamByStatusAsync_YieldsAllMatching()
    {
        var repo = new ShipmentRepository(_context);

        var count = 0;
        await foreach (var shipment in repo.StreamByStatusAsync(ShipmentStatus.Created))
        {
            Assert.Equal(ShipmentStatus.Created, shipment.Status);
            count++;
        }

        Assert.Equal(5, count);
    }
}
