using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Logistics.Infrastructure.Persistence.Neo4j;
using Logistics.Infrastructure.Persistence.Neo4j.Repositories;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Spins up a real Neo4j in Docker via Testcontainers, so repository Cypher is tested
/// against the actual engine. Requires Docker to be running.
/// </summary>
public class RouteRepositoryTests : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder("neo4j:5-community").Build();
    private Neo4jContext _context = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var settings = Options.Create(new Neo4jSettings
        {
            Uri = _container.GetConnectionString(),
            Username = "neo4j",
            Password = "neo4j",   // default for the Testcontainers image
            Database = "neo4j"
        });
        _context = new Neo4jContext(settings);

        // Seed two warehouses the route will connect.
        await using var session = _context.Session(AccessMode.Write);
        await session.RunAsync(
            "CREATE (:Warehouse {id:'w1', name:'A'}), (:Warehouse {id:'w2', name:'B'})");
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsRoute()
    {
        var repo = new RouteRepository(_context);
        var route = Route.Create("w1", "w2", 42, 99m, TransportMode.Rail);

        var id = await repo.AddAsync(route);
        var loaded = await repo.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("w1", loaded!.OriginId);
        Assert.Equal(42, loaded.DistanceKm);
    }
}
