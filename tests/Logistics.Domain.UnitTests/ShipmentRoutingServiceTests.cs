using Logistics.Domain.Enums;
using Logistics.Domain.Services;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class ShipmentRoutingServiceTests
{
    // Fake adapter standing in for the database — the service is DB-agnostic.
    private sealed class FakeRouteGraph(GraphPath? path) : IRouteGraph
    {
        public Task<GraphPath?> FindShortestPathAsync(string o, string d, CancellationToken ct = default)
            => Task.FromResult(path);
    }

    private static ITransportModeProfileResolver Resolver() =>
        new TransportModeProfileResolver(new ITransportModeProfile[]
        {
            new RoadProfile(), new RailProfile(), new SeaProfile(), new AirProfile(), new IntermodalProfile()
        });

    [Fact]
    public async Task EstimateAsync_ComputesDurationAndCostFromModeProfile()
    {
        var path = new GraphPath(new[] { "w1", "w2" }, TotalDistanceKm: 140, Hops: 1);
        var service = new ShipmentRoutingService(new FakeRouteGraph(path), Resolver());

        var estimate = await service.EstimateAsync("w1", "w2", TransportMode.Road);

        Assert.NotNull(estimate);
        // Road profile: 70 km/h, $1.20/km → 2h, $168.
        Assert.Equal(2.0, estimate!.EstimatedDuration.TotalHours, 3);
        Assert.Equal(168m, estimate.EstimatedCost);
    }

    [Fact]
    public async Task EstimateAsync_DifferentModesProduceDifferentEstimates()
    {
        var path = new GraphPath(new[] { "w1", "w2" }, 800, 1);
        var service = new ShipmentRoutingService(new FakeRouteGraph(path), Resolver());

        var air = await service.EstimateAsync("w1", "w2", TransportMode.Air);
        var sea = await service.EstimateAsync("w1", "w2", TransportMode.Sea);

        Assert.True(air!.EstimatedDuration < sea!.EstimatedDuration); // air is faster
        Assert.True(air.EstimatedCost > sea.EstimatedCost);           // air is pricier
    }

    [Fact]
    public async Task EstimateAsync_NoPath_ReturnsNull()
    {
        var service = new ShipmentRoutingService(new FakeRouteGraph(null), Resolver());
        Assert.Null(await service.EstimateAsync("w1", "w9", TransportMode.Rail));
    }
}
