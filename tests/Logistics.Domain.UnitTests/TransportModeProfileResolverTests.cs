using Logistics.Domain.Enums;
using Logistics.Domain.Exceptions;
using Logistics.Domain.Services;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class TransportModeProfileResolverTests
{
    [Theory]
    [InlineData(TransportMode.Road)]
    [InlineData(TransportMode.Rail)]
    [InlineData(TransportMode.Sea)]
    [InlineData(TransportMode.Air)]
    [InlineData(TransportMode.Intermodal)]
    public void Resolve_ReturnsMatchingProfile(TransportMode mode)
    {
        var resolver = new TransportModeProfileResolver(new ITransportModeProfile[]
        {
            new RoadProfile(), new RailProfile(), new SeaProfile(), new AirProfile(), new IntermodalProfile()
        });

        Assert.Equal(mode, resolver.Resolve(mode).Mode);
    }

    [Fact]
    public void Resolve_UnregisteredMode_Throws()
    {
        // Only Road registered resolving anything else fails fast.
        var resolver = new TransportModeProfileResolver(new ITransportModeProfile[] { new RoadProfile() });
        Assert.Throws<DomainException>(() => resolver.Resolve(TransportMode.Air));
    }
}
