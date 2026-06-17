using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace Logistics.Infrastructure.Messaging;

/// <summary>Used when the Kafka event bus is disabled — events are dropped (with a debug log).</summary>
public sealed class NoOpIntegrationEventPublisher(ILogger<NoOpIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        logger.LogDebug("Event bus disabled; dropped {EventType}", integrationEvent.EventType);
        return Task.CompletedTask;
    }
}

/// <summary>Used when the RabbitMQ notification bus is disabled — notifications are dropped.</summary>
public sealed class NoOpNotificationPublisher(ILogger<NoOpNotificationPublisher> logger)
    : INotificationPublisher
{
    public Task PublishAsync(NotificationMessage message, CancellationToken ct = default)
    {
        logger.LogDebug("Notification bus disabled; dropped message to {Recipient}", message.Recipient);
        return Task.CompletedTask;
    }
}
