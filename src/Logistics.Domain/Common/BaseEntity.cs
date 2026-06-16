namespace Logistics.Domain.Common;

/// <summary>
/// Base type for all domain entities. Identity is a string to map cleanly
/// onto Neo4j node element ids / business keys.
/// </summary>
public abstract class BaseEntity
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString("N");

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearEvents() => _domainEvents.Clear();
}

/// <summary>Marker for aggregate roots (the only entities a repository returns).</summary>
public interface IAggregateRoot { }

/// <summary>Base record for domain events.</summary>
public abstract record DomainEvent
{
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
