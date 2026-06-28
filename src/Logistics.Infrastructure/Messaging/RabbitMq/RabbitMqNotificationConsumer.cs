using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Logistics.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Consumes the notifications queue and delivers each message through the matching
/// <see cref="INotificationChannel"/> (email, SMS, …) resolved by <c>message.Channel</c>. Manual
/// ack after successful delivery gives at-least-once semantics; a failed delivery is nacked and
/// requeued (bounded by the queue's delivery limit, after which it dead-letters to the DLQ).
/// </summary>
public sealed class RabbitMqNotificationConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqNotificationConsumer> logger) : BackgroundService
{
    private IChannel? _channel;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
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
            if (message is null)
            {
                logger.LogWarning("Empty notification payload; dropping");
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            // Resolve the delivery channel per message (fresh scope keeps the typed SMS HttpClient
            // out of this singleton — same pattern as the domain-event worker).
            using var scope = scopeFactory.CreateScope();
            var channel = scope.ServiceProvider.GetServices<INotificationChannel>()
                .FirstOrDefault(c => string.Equals(c.Channel, message.Channel, StringComparison.OrdinalIgnoreCase));

            if (channel is null)
                logger.LogWarning("No delivery channel for '{Channel}'; dropping", message.Channel);
            else
                await channel.SendAsync(message, _stoppingToken);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Requeue for another attempt; the queue's x-delivery-limit caps retries and the broker
            // dead-letters the message to the DLQ once exhausted (no infinite poison loop).
            logger.LogError(ex, "Notification delivery failed; requeuing (bounded by delivery limit)");
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
