using System.Threading.Channels;
using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Common;

namespace Logistics.Infrastructure.Messaging;

/// <summary>
/// In-memory bounded channel backing <see cref="IDomainEventQueue"/>. Registered as a
/// singleton (the channel must be shared across all producers and the single consumer).
///
/// Bounded + <see cref="BoundedChannelFullMode.Wait"/> gives back-pressure: if events are
/// produced faster than they're handled, producers await rather than exhausting memory.
/// The channel itself is lock-free and safe for many concurrent writers / one reader.
/// </summary>
public sealed class ChannelDomainEventQueue : IDomainEventQueue
{
    private readonly Channel<DomainEvent> _channel = Channel.CreateBounded<DomainEvent>(
        new BoundedChannelOptions(capacity: 1024)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(DomainEvent domainEvent, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(domainEvent, ct);

    public IAsyncEnumerable<DomainEvent> DequeueAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
