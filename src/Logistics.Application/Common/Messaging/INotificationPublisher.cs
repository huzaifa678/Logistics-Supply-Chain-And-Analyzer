namespace Logistics.Application.Common.Messaging;

/// <summary>A notification to be delivered to a recipient (email/SMS/push — channel-agnostic).</summary>
public sealed record NotificationMessage(string Channel, string Recipient, string Subject, string Body)
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
}

/// <summary>
/// Enqueues notifications onto the notification bus (RabbitMQ by default). Separate from the
/// integration-event backbone: this is a work queue for delivery, not an event stream.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(NotificationMessage message, CancellationToken ct = default);
}
