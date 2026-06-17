using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Messaging;
using RabbitMQ.Client;

namespace Logistics.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// RabbitMQ implementation of <see cref="INotificationPublisher"/>. Publishes persistent JSON
/// messages to the durable notifications queue (default exchange, routing key = queue name).
/// </summary>
public sealed class RabbitMqNotificationPublisher(RabbitMqConnection connection) : INotificationPublisher
{
    public async Task PublishAsync(NotificationMessage message, CancellationToken ct = default)
    {
        await using var channel = await connection.CreateChannelAsync(ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.MessageId
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: connection.Queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }
}
