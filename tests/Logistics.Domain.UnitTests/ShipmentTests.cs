using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Logistics.Domain.Events;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class ShipmentTests
{
    private static Shipment NewShipment() =>
        Shipment.Create("TRK-1", "w1", "w2", 250, TransportMode.Sea);

    [Fact]
    public void Create_StartsInCreatedStatus()
    {
        Assert.Equal(ShipmentStatus.Created, NewShipment().Status);
    }

    [Fact]
    public void Dispatch_FromCreated_MovesToInTransit()
    {
        var s = NewShipment();
        s.Dispatch(DateTimeOffset.UtcNow.AddDays(3));
        Assert.Equal(ShipmentStatus.InTransit, s.Status);
        Assert.NotNull(s.EstimatedArrival);
    }

    [Fact]
    public void MarkDelayed_RaisesDomainEvent()
    {
        var s = NewShipment();
        s.Dispatch(DateTimeOffset.UtcNow.AddDays(3));
        s.MarkDelayed("Port congestion");

        Assert.Equal(ShipmentStatus.Delayed, s.Status);
        Assert.Single(s.DomainEvents);
        Assert.IsType<ShipmentDelayedEvent>(s.DomainEvents.First());
    }

    [Fact]
    public void Deliver_BeforeDispatch_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewShipment().Deliver());
    }
}
