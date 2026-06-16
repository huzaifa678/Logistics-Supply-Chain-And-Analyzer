using Logistics.Domain.Events;
using Logistics.Infrastructure.Messaging;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Pure in-memory concurrency test — no Docker required.</summary>
public class ChannelDomainEventQueueTests
{
    [Fact]
    public async Task Enqueue_ThenDequeue_RoundTripsInOrder()
    {
        var queue = new ChannelDomainEventQueue();

        await queue.EnqueueAsync(new ShipmentDelayedEvent("s1", "TRK-1", "a"));
        await queue.EnqueueAsync(new ShipmentDelayedEvent("s2", "TRK-2", "b"));

        var received = new List<string>();

        await foreach (var evt in queue.DequeueAllAsync())
        {
            received.Add(((ShipmentDelayedEvent)evt).ShipmentId);
            if (received.Count == 2) break; // stop consuming
        }

        Assert.Equal(new[] { "s1", "s2" }, received);
    }
}
