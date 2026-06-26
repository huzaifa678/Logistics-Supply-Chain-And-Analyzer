using Logistics.Domain.Entities;
using Logistics.Domain.Events;
using Logistics.Domain.ValueObjects;
using Xunit;

namespace Logistics.Domain.UnitTests;

public class WarehouseTests
{
    [Fact]
    public void Create_RaisesWarehouseCreatedEvent()
    {
        var warehouse = Warehouse.Create("Central Hub", new GeoLocation(10, 20), 100);

        var evt = Assert.Single(warehouse.DomainEvents);
        var created = Assert.IsType<WarehouseCreatedEvent>(evt);
        Assert.Equal(warehouse.Id, created.WarehouseId);
        Assert.Equal("Central Hub", created.Name);
    }

    [Fact]
    public void Create_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Warehouse.Create(" ", new GeoLocation(0, 0), 0));
    }
}
