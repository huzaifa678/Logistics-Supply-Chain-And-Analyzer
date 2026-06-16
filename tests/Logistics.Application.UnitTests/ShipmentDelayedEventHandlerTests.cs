using Logistics.Application.Shipments.EventHandlers;
using Logistics.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Logistics.Application.UnitTests;

public class ShipmentDelayedEventHandlerTests
{
    private readonly ShipmentDelayedEventHandler _handler =
        new(NullLogger<ShipmentDelayedEventHandler>.Instance);

    [Fact]
    public void CanHandle_OnlyShipmentDelayedEvent()
    {
        Assert.True(_handler.CanHandle(new ShipmentDelayedEvent("s1", "TRK", "late")));
    }

    private sealed record OtherEvent : Logistics.Domain.Common.DomainEvent;

    [Fact]
    public void CanHandle_IgnoresOtherEvents()
    {
        Assert.False(_handler.CanHandle(new OtherEvent()));
    }

    [Fact]
    public async Task HandleAsync_Completes()
    {
        await _handler.HandleAsync(new ShipmentDelayedEvent("s1", "TRK", "late"), default);
    }
}
