using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.Kafka.Serialization;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>
/// Kafka implementation of <see cref="IIntegrationEventPublisher"/>, serializing events as
/// <b>Avro</b> against the Schema Registry. The serializer auto-registers the schema under the
/// subject <c>{topic}-value</c> and prefixes each payload with the schema id, so consumers
/// decode against the exact registered schema (and compatibility is enforced on evolution).
/// One producer for the app lifetime; the registry client is shared (owned by DI).
/// </summary>
public sealed class KafkaEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private readonly IProducer<string, GenericRecord> _producer;
    private readonly string _topic;

    public KafkaEventPublisher(IOptions<KafkaSettings> options, ISchemaRegistryClient schemaRegistry)
    {
        var settings = options.Value;
        _topic = settings.Topic;
        _producer = new ProducerBuilder<string, GenericRecord>(
                new ProducerConfig { BootstrapServers = settings.BootstrapServers })
            .SetValueSerializer(new AvroSerializer<GenericRecord>(
                schemaRegistry, new AvroSerializerConfig { AutoRegisterSchemas = true }))
            .Build();
    }

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        var message = new Message<string, GenericRecord>
        {
            Key = integrationEvent.EventType,
            Value = IntegrationEventAvroMapper.ToRecord(integrationEvent)
        };
        await _producer.ProduceAsync(_topic, message, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
