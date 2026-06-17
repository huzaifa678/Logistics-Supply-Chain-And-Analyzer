using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Logistics.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Consumes the notifications queue and "delivers" each message (here, logs it — this is where a
/// real email/SMS/push gateway would plug in). Manual ack after successful delivery gives
/// at-least-once semantics; a failed delivery is nacked and requeued.
/// </summary>
public sealed class RabbitMqNotificationConsumer(
    RabbitMqConnection connection,
    ILogger<RabbitMqNotificationConsumer> logger) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: connection.Queue, autoAck: false, consumerTag: string.Empty,
            noLocal: false, exclusive: false, arguments: null,
            consumer: consumer, cancellationToken: stoppingToken);

        logger.LogInformation("RabbitMQ notification consumer listening on {Queue}.", connection.Queue);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var message = JsonSerializer.Deserialize<NotificationMessage>(json);

            logger.LogInformation("Delivering {Channel} notification to {Recipient}: {Subject}",
                message?.Channel, message?.Recipient, message?.Subject);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification delivery failed; requeuing");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
