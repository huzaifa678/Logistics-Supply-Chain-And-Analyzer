using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Logistics.Domain.Services;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class ShipmentRiskServiceTests
{
    private sealed class FakeRouteGraph(GraphPath? path) : IRouteGraph
    {
        public Task<GraphPath?> FindShortestPathAsync(string o, string d, CancellationToken ct = default)
            => Task.FromResult(path);
    }

    private sealed class FakeCongestion(int count) : IWarehouseCongestionProvider
    {
        public Task<int> GetActiveShipmentCountAsync(string warehouseId, CancellationToken ct = default)
            => Task.FromResult(count);
    }

    private static IRiskFactor[] AllFactors() =>
    [
        new DistanceRiskFactor(), new HopCountRiskFactor(), new TransportModeRiskFactor(),
        new DelayRiskFactor(), new CongestionRiskFactor()
    ];

    private static Shipment NewShipment(TransportMode mode = TransportMode.Road) =>
        Shipment.Create("TRK-1", "w1", "w2", "+15551230002", 100, mode);

    [Fact]
    public async Task AssessAsync_LowRisk_ShortRoadHop_NoCongestion()
    {
        var path = new GraphPath(new[] { "w1", "w2" }, TotalDistanceKm: 50, Hops: 1);
        var service = new ShipmentRiskService(new FakeRouteGraph(path), new FakeCongestion(0), AllFactors());

        var assessment = await service.AssessAsync(NewShipment());

        Assert.NotNull(assessment);
        Assert.Equal(RiskBand.Low, assessment!.Band);
        Assert.Equal(5, assessment.Factors.Count);
    }

    [Fact]
    public async Task AssessAsync_HigherRisk_LongMultiHopSea_WhenDelayedAndCongested()
    {
        var path = new GraphPath(new[] { "w1", "w2", "w3", "w4" }, TotalDistanceKm: 1800, Hops: 3);
        var shipment = NewShipment(TransportMode.Sea);
        shipment.Dispatch(DateTimeOffset.UtcNow.AddDays(5));
        shipment.MarkDelayed("storm");

        var service = new ShipmentRiskService(new FakeRouteGraph(path), new FakeCongestion(30), AllFactors());

        var assessment = await service.AssessAsync(shipment);

        Assert.NotNull(assessment);
        Assert.True(assessment!.Score >= 66, $"expected High band, got {assessment.Score}");
        Assert.Equal(RiskBand.High, assessment.Band);
    }

    [Fact]
    public async Task AssessAsync_ScoreIsClampedTo100()
    {
        var path = new GraphPath(new[] { "w1", "w2" }, TotalDistanceKm: 100_000, Hops: 50);
        var service = new ShipmentRiskService(new FakeRouteGraph(path), new FakeCongestion(1000),
            AllFactors());

        var assessment = await service.AssessAsync(NewShipment(TransportMode.Sea));

        Assert.True(assessment!.Score <= 100);
    }

    [Fact]
    public async Task AssessAsync_NoRoute_ReturnsNull()
    {
        var service = new ShipmentRiskService(new FakeRouteGraph(null), new FakeCongestion(0), AllFactors());
        Assert.Null(await service.AssessAsync(NewShipment()));
    }
}
