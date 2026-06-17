using System.Text.Json;
using Confluent.Kafka;
using Logistics.Application.Common.Messaging;
using Logistics.Application.Shipments.IntegrationEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>
/// Consumes the integration-event topic and bridges relevant events to the notification bus:
/// a <see cref="ShipmentDelayedIntegrationEvent"/> becomes a customer notification published to
/// RabbitMQ. This is the Kafka → RabbitMQ hop of the pipeline.
///
/// Kafka's consumer is blocking, so the poll loop runs on a dedicated background thread
/// (Task.Run) and commits offsets only after the notification is published (at-least-once).
/// </summary>
public sealed class KafkaIntegrationEventConsumer(
    INotificationPublisher notifications,
    IOptions<KafkaSettings> options,
    ILogger<KafkaIntegrationEventConsumer> logger) : BackgroundService
{
    private readonly KafkaSettings _settings = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_settings.Topic);
        logger.LogInformation("Kafka consumer subscribed to {Topic}.", _settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error");
                    continue;
                }

                await HandleAsync(result.Message, stoppingToken);
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleAsync(Message<string, string> message, CancellationToken ct)
    {
        if (message.Key != nameof(ShipmentDelayedIntegrationEvent))
            return; // not interested

        var e = JsonSerializer.Deserialize<ShipmentDelayedIntegrationEvent>(message.Value);
        if (e is null) return;

        await notifications.PublishAsync(new NotificationMessage(
            Channel: "email",
            Recipient: "ops@logistics.example",
            Subject: $"Shipment {e.TrackingNumber} delayed",
            Body: $"Shipment {e.ShipmentId} is delayed: {e.Reason}"), ct);

        logger.LogInformation("Queued delay notification for shipment {TrackingNumber}.", e.TrackingNumber);
    }
}
