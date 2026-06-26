using System.Text;
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
        // Raw producer for the dead-letter topic: forwards poison/failed messages as bytes.
        using var deadLetter = new ProducerBuilder<byte[], byte[]>(
            new ProducerConfig { BootstrapServers = _settings.BootstrapServers }).Build();
        var dltTopic = _settings.ResolvedDeadLetterTopic;

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
                    // Undeserializable (poison) message → dead-letter the raw bytes and skip past it
                    // so the partition isn't blocked forever.
                    var cr = ex.ConsumerRecord;
                    logger.LogError(ex, "Kafka deserialization failed at {Offset}; dead-lettering",
                        cr?.TopicPartitionOffset);
                    if (cr is not null)
                    {
                        await DeadLetterAsync(deadLetter, dltTopic, cr.Message?.Key, cr.Message?.Value,
                            ex.Error.Reason, cr.TopicPartitionOffset, stoppingToken);
                        consumer.Commit(new[]
                        {
                            new TopicPartitionOffset(cr.TopicPartition, new Offset(cr.Offset.Value + 1)),
                        });
                    }
                    continue;
                }

                try
                {
                    await HandleAsync(result.Message.Value, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Processing failed → dead-letter a best-effort copy, then commit so we move on.
                    logger.LogError(ex, "Processing failed at {Offset}; dead-lettering",
                        result.TopicPartitionOffset);
                    var value = Encoding.UTF8.GetBytes(result.Message.Value?.ToString() ?? string.Empty);
                    var key = result.Message.Key is null ? null : Encoding.UTF8.GetBytes(result.Message.Key);
                    await DeadLetterAsync(deadLetter, dltTopic, key, value, ex.Message,
                        result.TopicPartitionOffset, stoppingToken);
                }

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

    /// <summary>Produce a message to the dead-letter topic, tagging it with the failure reason/origin.</summary>
    private static async Task DeadLetterAsync(
        IProducer<byte[], byte[]> producer, string topic, byte[]? key, byte[]? value,
        string reason, TopicPartitionOffset origin, CancellationToken ct)
    {
        var headers = new Headers
        {
            { "x-dlt-reason", Encoding.UTF8.GetBytes(reason) },
            { "x-dlt-origin", Encoding.UTF8.GetBytes(origin.ToString()) },
        };
        await producer.ProduceAsync(
            topic, new Message<byte[], byte[]> { Key = key ?? [], Value = value ?? [], Headers = headers }, ct);
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
