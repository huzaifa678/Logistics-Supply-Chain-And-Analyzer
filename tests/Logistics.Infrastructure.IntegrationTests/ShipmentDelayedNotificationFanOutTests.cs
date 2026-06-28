using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Logistics.Application.Common.Messaging;
using Logistics.Application.Shipments.IntegrationEvents;
using Logistics.Infrastructure.Messaging.Kafka;
using Logistics.Infrastructure.Messaging.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Redpanda;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// A delayed shipment carrying a customer phone fans out to three notifications: an ops email,
/// an SMS to the customer, and an SMS to the ops line. Runs the real publisher + consumer against
/// Redpanda (Kafka + Schema Registry). Needs Docker.
/// </summary>
public class ShipmentDelayedNotificationFanOutTests : IAsyncLifetime
{
    private readonly RedpandaContainer _container = new RedpandaBuilder("redpandadata/redpanda:v24.2.7").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private sealed class RecordingNotifications : INotificationPublisher
    {
        public ConcurrentBag<NotificationMessage> Messages { get; } = new();

        public Task PublishAsync(NotificationMessage message, CancellationToken ct = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DelayedShipment_FansOutToCustomerAndOpsSms()
    {
        const string customerPhone = "+15557654321";
        const string opsPhone = "+15550009999";

        var bootstrap = _container.GetBootstrapAddress();
        var schemaRegistryUrl = _container.GetSchemaRegistryAddress();

        using var schemaRegistry = new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = schemaRegistryUrl });

        // Publish a real ShipmentDelayed event onto the topic the consumer reads.
        using (var publisher = new KafkaEventPublisher(
                   Options.Create(new KafkaSettings
                   {
                       BootstrapServers = bootstrap,
                       Topic = "test.events",
                       SchemaRegistryUrl = schemaRegistryUrl,
                   }),
                   schemaRegistry))
        {
            await publisher.PublishAsync(
                new ShipmentDelayedIntegrationEvent("s1", "TRK-1", "weather", customerPhone));
        }

        var notifications = new RecordingNotifications();
        var consumer = new KafkaIntegrationEventConsumer(
            notifications,
            schemaRegistry,
            Options.Create(new KafkaSettings
            {
                BootstrapServers = bootstrap,
                Topic = "test.events",
                ConsumerGroup = "test-fanout",
                SchemaRegistryUrl = schemaRegistryUrl,
                DeadLetterTopic = "test.events.DLT",
            }),
            Options.Create(new NotificationSettings { OpsEmail = "ops@logistics.test", OpsPhone = opsPhone }),
            NullLogger<KafkaIntegrationEventConsumer>.Instance);

        await consumer.StartAsync(CancellationToken.None);

        // Poll until the three fan-out messages have been published (or time out).
        var deadline = DateTime.UtcNow.AddSeconds(40);
        while (DateTime.UtcNow < deadline && notifications.Messages.Count < 3)
            await Task.Delay(250);

        await consumer.StopAsync(CancellationToken.None);

        var messages = notifications.Messages.ToList();
        Assert.Contains(messages, m => m.Channel == "email" && m.Recipient == "ops@logistics.test");
        Assert.Contains(messages, m => m.Channel == "sms" && m.Recipient == customerPhone);
        Assert.Contains(messages, m => m.Channel == "sms" && m.Recipient == opsPhone);
    }
}
