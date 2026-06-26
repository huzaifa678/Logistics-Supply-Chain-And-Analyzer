using Logistics.Application.Common.Webhooks;
using Logistics.Domain.Common;
using Logistics.Domain.Events;
using Xunit;

namespace Logistics.Application.UnitTests;

public class WebhookDispatchHandlerTests
{
    private sealed class CapturingSender : IWebhookSender
    {
        public List<WebhookEvent> Sent { get; } = new();

        public Task SendAsync(WebhookEvent webhookEvent, CancellationToken ct = default)
        {
            Sent.Add(webhookEvent);
            return Task.CompletedTask;
        }
    }

    private sealed record UnrelatedEvent : DomainEvent;

    [Fact]
    public void CanHandle_SupportedEventsOnly()
    {
        var handler = new WebhookDispatchHandler(new CapturingSender());

        Assert.True(handler.CanHandle(new ShipmentCreatedEvent("s1", "TRK")));
        Assert.True(handler.CanHandle(new ShipmentDelayedEvent("s1", "TRK", "late")));
        Assert.True(handler.CanHandle(new WarehouseCreatedEvent("w1", "Hub")));
        Assert.False(handler.CanHandle(new UnrelatedEvent()));
    }

    [Fact]
    public async Task HandleAsync_MapsEachEventToItsType()
    {
        var sender = new CapturingSender();
        var handler = new WebhookDispatchHandler(sender);

        await handler.HandleAsync(new ShipmentCreatedEvent("s1", "TRK"), default);
        await handler.HandleAsync(new ShipmentDelayedEvent("s1", "TRK", "late"), default);
        await handler.HandleAsync(new WarehouseCreatedEvent("w1", "Hub"), default);

        Assert.Equal(
            new[] { "shipment.created", "shipment.delayed", "warehouse.created" },
            sender.Sent.Select(e => e.EventType));
    }

    [Fact]
    public async Task HandleAsync_UnrelatedEvent_DoesNotSend()
    {
        var sender = new CapturingSender();
        var handler = new WebhookDispatchHandler(sender);

        await handler.HandleAsync(new UnrelatedEvent(), default);

        Assert.Empty(sender.Sent);
    }
}
