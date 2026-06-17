using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Logistics.Application.Shipments.IntegrationEvents;
using Logistics.Infrastructure.Messaging.Kafka;
using Logistics.Infrastructure.Messaging.Kafka.Serialization;
using Microsoft.Extensions.Options;
using Testcontainers.Redpanda;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// End-to-end Avro + Schema Registry round-trip against real Redpanda (Kafka API + built-in
/// Schema Registry in one container). Needs Docker.
/// </summary>
public class KafkaEventPublisherTests : IAsyncLifetime
{
    private readonly RedpandaContainer _container = new RedpandaBuilder("redpandadata/redpanda:v24.2.7").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task PublishAsync_ProducesAvroConsumableViaSchemaRegistry()
    {
        var bootstrap = _container.GetBootstrapAddress();
        var schemaRegistryUrl = _container.GetSchemaRegistryAddress();

        using var schemaRegistry = new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = schemaRegistryUrl });

        var settings = Options.Create(new KafkaSettings
        {
            BootstrapServers = bootstrap,
            Topic = "test.events",
            SchemaRegistryUrl = schemaRegistryUrl
        });

        using (var publisher = new KafkaEventPublisher(settings, schemaRegistry))
        {
            await publisher.PublishAsync(new ShipmentDelayedIntegrationEvent("s1", "TRK-1", "weather"));
        }

        using var consumer = new ConsumerBuilder<string, GenericRecord>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "test-reader",
            AutoOffsetReset = AutoOffsetReset.Earliest
        })
            .SetValueDeserializer(new AvroDeserializer<GenericRecord>(schemaRegistry).AsSyncOverAsync())
            .Build();
        consumer.Subscribe("test.events");

        var result = consumer.Consume(TimeSpan.FromSeconds(30));
        consumer.Close();

        Assert.NotNull(result);
        Assert.Equal(ShipmentDelayedAvro.RecordName, result!.Message.Value.Schema.Name);

        var decoded = ShipmentDelayedAvro.FromRecord(result.Message.Value);
        Assert.Equal("TRK-1", decoded.TrackingNumber);
        Assert.Equal("weather", decoded.Reason);
    }
}
