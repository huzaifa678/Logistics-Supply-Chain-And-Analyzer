using System.Text;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.Kafka.Serialization;
using Logistics.Infrastructure.Messaging.Notifications;
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
    IOptions<NotificationSettings> notificationOptions,
    ILogger<KafkaIntegrationEventConsumer> logger) : BackgroundService
{
    private readonly KafkaSettings _settings = options.Value;
    private readonly NotificationSettings _recipients = notificationOptions.Value;

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
            
        using var deadLetter = new ProducerBuilder<byte[], byte[]>(
            new ProducerConfig { BootstrapServers = _settings.BootstrapServers }).Build();
        var dltTopic = _settings.ResolvedDeadLetterTopic;

        await EnsureTopicsAsync(_settings.Topic, dltTopic, stoppingToken);

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
                    var cr = ex.ConsumerRecord;

                    // Broker/transport errors (e.g. topic momentarily unavailable, leader election)
                    // carry no message payload — they are NOT poison messages. Dead-lettering them
                    // would produce to a topic that may itself be unavailable and crash the loop, so
                    // just log and back off; librdkafka retries the fetch.
                    if (cr?.Message?.Value is null)
                    {
                        logger.LogWarning(ex, "Kafka consume error at {Offset}; retrying",
                            cr?.TopicPartitionOffset);
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    // Undeserializable (poison) message → dead-letter the raw bytes and skip past it
                    // so the partition isn't blocked forever.
                    logger.LogError(ex, "Kafka deserialization failed at {Offset}; dead-lettering",
                        cr.TopicPartitionOffset);
                    await DeadLetterAsync(deadLetter, dltTopic, cr.Message.Key, cr.Message.Value,
                        ex.Error.Reason, cr.TopicPartitionOffset, stoppingToken);
                    consumer.Commit(new[]
                    {
                        new TopicPartitionOffset(cr.TopicPartition, new Offset(cr.Offset.Value + 1)),
                    });
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

    /// <summary>
    /// Idempotently create the given topics. Existing topics are left untouched
    /// (TopicAlreadyExists is ignored), so this is safe to run on every startup.
    /// </summary>
    private async Task EnsureTopicsAsync(string topic, string deadLetterTopic, CancellationToken ct)
    {
        using var admin = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _settings.BootstrapServers }).Build();

        var specs = new[] { topic, deadLetterTopic }
            .Select(name => new TopicSpecification { Name = name, NumPartitions = 1, ReplicationFactor = 1 })
            .ToList();

        try
        {
            await admin.CreateTopicsAsync(specs);
            logger.LogInformation("Ensured Kafka topics: {Topic}, {DeadLetterTopic}.", topic, deadLetterTopic);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r =>
                   r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            // All topics already exist (or were just created) — nothing to do.
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

        var subject = $"Shipment {e.TrackingNumber} delayed";
        var body = $"Shipment {e.ShipmentId} is delayed: {e.Reason}";

        // Fan out: always an email to ops, plus an SMS to the affected customer and to ops.
        await notifications.PublishAsync(
            new NotificationMessage("email", _recipients.OpsEmail, subject, body), ct);

        if (!string.IsNullOrWhiteSpace(e.CustomerPhone))
            await notifications.PublishAsync(
                new NotificationMessage("sms", e.CustomerPhone, subject, body), ct);

        if (!string.IsNullOrWhiteSpace(_recipients.OpsPhone))
            await notifications.PublishAsync(
                new NotificationMessage("sms", _recipients.OpsPhone, subject, body), ct);

        logger.LogInformation("Queued delay notifications for shipment {TrackingNumber}.", e.TrackingNumber);
    }
}
