namespace Logistics.Application.Common.Messaging;

/// <summary>
/// Delivers a notification over one transport (email, SMS, …). The consumer routes each
/// <see cref="NotificationMessage"/> to the channel whose <see cref="Channel"/> matches
/// <see cref="NotificationMessage.Channel"/>. New transports plug in without touching the consumer.
/// </summary>
public interface INotificationChannel
{
    /// <summary>The channel name this implementation handles, e.g. "email" or "sms".</summary>
    string Channel { get; }

    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}
