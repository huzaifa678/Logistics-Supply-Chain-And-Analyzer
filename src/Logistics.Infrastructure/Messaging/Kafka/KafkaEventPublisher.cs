using System.Text.Json;
using Confluent.Kafka;
using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka implementation of <see cref="IIntegrationEventPublisher"/>. One producer for the app
/// lifetime (thread-safe, batches internally). The event's type name is the message key, so all
/// events for a logical key land on the same partition (ordering) and consumers can route by key.
/// </summary>
public sealed class KafkaEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaEventPublisher(IOptions<KafkaSettings> options)
    {
        var settings = options.Value;
        _producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = settings.BootstrapServers }).Build();
        _topic = settings.Topic;
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        var message = new Message<string, string>
        {
            Key = integrationEvent.EventType,
            Value = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType())
        };
        await _producer.ProduceAsync(_topic, message, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
