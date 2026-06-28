using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Redpanda;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies a poison (non-Avro) message is forwarded to the dead-letter topic instead of blocking
/// the consumer. Runs the real consumer against Redpanda (Kafka + Schema Registry). Needs Docker.
/// </summary>
public class KafkaDeadLetterTests : IAsyncLifetime
{
    private readonly RedpandaContainer _container = new RedpandaBuilder("redpandadata/redpanda:v24.2.7").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private sealed class NoOpNotifications : INotificationPublisher
    {
        public Task PublishAsync(NotificationMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task PoisonMessage_IsForwardedToDeadLetterTopic()
    {
        var bootstrap = _container.GetBootstrapAddress();
        var schemaRegistryUrl = _container.GetSchemaRegistryAddress();

        // Produce a non-Avro (poison) message — the Avro deserializer will reject it.
        using (var producer = new ProducerBuilder<Null, byte[]>(
                   new ProducerConfig { BootstrapServers = bootstrap }).Build())
        {
            await producer.ProduceAsync("test.events",
                new Message<Null, byte[]> { Value = new byte[] { 9, 9, 9, 9, 9 } });
            producer.Flush(TimeSpan.FromSeconds(10));
        }

        using var schemaRegistry = new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = schemaRegistryUrl });
        var consumer = new KafkaIntegrationEventConsumer(
            new NoOpNotifications(),
            schemaRegistry,
            Options.Create(new KafkaSettings
            {
                BootstrapServers = bootstrap,
                Topic = "test.events",
                ConsumerGroup = "test-dlt",
                SchemaRegistryUrl = schemaRegistryUrl,
                DeadLetterTopic = "test.events.DLT",
            }),
            Options.Create(new Logistics.Infrastructure.Messaging.Notifications.NotificationSettings()),
            NullLogger<KafkaIntegrationEventConsumer>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await consumer.StartAsync(cts.Token);

        using var dltReader = new ConsumerBuilder<byte[], byte[]>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "test-dlt-reader",
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        dltReader.Subscribe("test.events.DLT");

        // Poll until the consumer has dead-lettered the poison message. The DLT topic is created
        // on first produce, so tolerate the transient "unknown topic" until it exists.
        ConsumeResult<byte[], byte[]>? result = null;
        var deadline = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < deadline && result is null)
        {
            try
            {
                result = dltReader.Consume(TimeSpan.FromSeconds(2));
            }
            catch (ConsumeException)
            {
                await Task.Delay(500);
            }
        }

        dltReader.Close();
        await consumer.StopAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(result!.Message.Headers, h => h.Key == "x-dlt-reason");
    }
}
