using Logistics.Domain.Common;

namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Producer/consumer boundary for domain events. Command handlers enqueue events after a
/// successful write; a background worker drains and dispatches them off the request thread.
/// Backed by a bounded in-memory channel — swap the implementation for an outbox/broker
/// without touching callers (Dependency Inversion).
/// </summary>
public interface IDomainEventQueue
{
    /// <summary>Enqueue an event. Awaits (back-pressure) if the channel is full.</summary>
    ValueTask EnqueueAsync(DomainEvent domainEvent, CancellationToken ct = default);

    /// <summary>Async stream the worker consumes; completes when the app shuts down.</summary>
    IAsyncEnumerable<DomainEvent> DequeueAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Handles one kind of domain event. New handlers are added without modifying the
/// dispatcher (Open/Closed) — they're discovered through DI.
/// </summary>
public interface IDomainEventHandler
{
    bool CanHandle(DomainEvent domainEvent);
    Task HandleAsync(DomainEvent domainEvent, CancellationToken ct);
}
