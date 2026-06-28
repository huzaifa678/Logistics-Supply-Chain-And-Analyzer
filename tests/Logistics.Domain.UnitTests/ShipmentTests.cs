using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Logistics.Domain.Events;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class ShipmentTests
{
    private static Shipment NewShipment() =>
        Shipment.Create("TRK-1", "w1", "w2", "+15551230001", 250, TransportMode.Sea);

    [Fact]
    public void Create_StartsInCreatedStatus()
    {
        Assert.Equal(ShipmentStatus.Created, NewShipment().Status);
    }

    [Fact]
    public void Create_RaisesShipmentCreatedEvent()
    {
        var shipment = NewShipment();
        var created = Assert.IsType<ShipmentCreatedEvent>(Assert.Single(shipment.DomainEvents));
        Assert.Equal(shipment.Id, created.ShipmentId);
        Assert.Equal("TRK-1", created.TrackingNumber);
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
        // DomainEvents also carries the ShipmentCreatedEvent from Create(); assert the delay is raised.
        Assert.Contains(s.DomainEvents, e => e is ShipmentDelayedEvent);
    }

    [Fact]
    public void MarkDelayed_CarriesCustomerPhone()
    {
        var s = NewShipment();
        s.Dispatch(DateTimeOffset.UtcNow.AddDays(3));
        s.MarkDelayed("Port congestion");

        var delayed = Assert.IsType<ShipmentDelayedEvent>(
            Assert.Single(s.DomainEvents, e => e is ShipmentDelayedEvent));
        Assert.Equal("+15551230001", delayed.CustomerPhone);
    }

    [Fact]
    public void Create_WithoutCustomerPhone_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Shipment.Create("TRK-1", "w1", "w2", "", 250, TransportMode.Sea));
    }

    [Fact]
    public void Deliver_BeforeDispatch_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => NewShipment().Deliver());
    }
}
