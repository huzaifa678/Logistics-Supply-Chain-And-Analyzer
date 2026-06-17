using Confluent.Kafka;
using Logistics.Application.Shipments.IntegrationEvents;
using Logistics.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Options;
using Testcontainers.Kafka;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Publishes an integration event to real Kafka and consumes it back (needs Docker).</summary>
public class KafkaEventPublisherTests : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder("confluentinc/cp-kafka:7.6.1").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task PublishAsync_IsConsumableFromTopic()
    {
        var bootstrap = _container.GetBootstrapAddress().Replace("PLAINTEXT://", "");
        var settings = Options.Create(new KafkaSettings
        {
            BootstrapServers = bootstrap,
            Topic = "test.events"
        });

        using (var publisher = new KafkaEventPublisher(settings))
        {
            await publisher.PublishAsync(
                new ShipmentDelayedIntegrationEvent("s1", "TRK-1", "weather"));
        }

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "test-reader",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        consumer.Subscribe("test.events");

        var result = consumer.Consume(TimeSpan.FromSeconds(30));
        consumer.Close();

        Assert.NotNull(result);
        Assert.Equal(nameof(ShipmentDelayedIntegrationEvent), result!.Message.Key);
        Assert.Contains("TRK-1", result.Message.Value);
    }
}
