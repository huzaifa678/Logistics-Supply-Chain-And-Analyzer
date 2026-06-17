using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.Kafka.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>
/// Consumes the Avro integration-event topic (decoding via the Schema Registry) and bridges
/// relevant events to the notification bus: a delayed shipment becomes a RabbitMQ notification.
/// This is the Kafka → RabbitMQ hop. Offsets are committed only after the notification is
/// published (at-least-once).
/// </summary>
public sealed class KafkaIntegrationEventConsumer(
    INotificationPublisher notifications,
    ISchemaRegistryClient schemaRegistry,
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

        using var consumer = new ConsumerBuilder<string, GenericRecord>(config)
            .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
            .Build();
        consumer.Subscribe(_settings.Topic);
        logger.LogInformation("Kafka (Avro) consumer subscribed to {Topic}.", _settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, GenericRecord> result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error");
                    continue;
                }

                await HandleAsync(result.Message.Value, stoppingToken);
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

    private async Task HandleAsync(GenericRecord record, CancellationToken ct)
    {
        if (record.Schema.Name != ShipmentDelayedAvro.RecordName)
            return; // not an event we act on

        var e = ShipmentDelayedAvro.FromRecord(record);

        await notifications.PublishAsync(new NotificationMessage(
            Channel: "email",
            Recipient: "ops@logistics.example",
            Subject: $"Shipment {e.TrackingNumber} delayed",
            Body: $"Shipment {e.ShipmentId} is delayed: {e.Reason}"), ct);

        logger.LogInformation("Queued delay notification for shipment {TrackingNumber}.", e.TrackingNumber);
    }
}
