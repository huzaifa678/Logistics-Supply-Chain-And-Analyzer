namespace Logistics.Application.Common.Messaging;

/// <summary>
/// Base for cross-service integration events published to the broker. Distinct from a
/// <c>DomainEvent</c> (which is internal to this service) — an integration event is a stable
/// contract other services consume.
/// </summary>
public abstract record IntegrationEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Logical type name used as the message key / routing discriminator.</summary>
    public string EventType => GetType().Name;
}
