namespace Logistics.Application.Common.Messaging;

/// <summary>
/// Publishes integration events to the event backbone (Kafka by default). Swappable: any broker
/// can satisfy this port without callers knowing (Dependency Inversion).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct = default);
}
