using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class RouteTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var route = Route.Create("w1", "w2", 120.5, 300m, TransportMode.Road);

        Assert.Equal("w1", route.OriginId);
        Assert.Equal("w2", route.DestinationId);
        Assert.Equal(120.5, route.DistanceKm);
        Assert.False(string.IsNullOrEmpty(route.Id));
    }

    [Fact]
    public void Create_WithSameOriginAndDestination_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Route.Create("w1", "w1", 10, 10m, TransportMode.Rail));
        Assert.Contains("same warehouse", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveDistance_Throws(double distance)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Route.Create("w1", "w2", distance, 10m, TransportMode.Air));
    }
}
