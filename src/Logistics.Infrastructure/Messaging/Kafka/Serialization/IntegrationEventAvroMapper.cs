using Avro.Generic;
using Logistics.Application.Common.Messaging;
using Logistics.Application.Shipments.IntegrationEvents;

namespace Logistics.Infrastructure.Messaging.Kafka.Serialization;

/// <summary>
/// Maps integration events to/from their Avro <see cref="GenericRecord"/> form. Adding a new
/// event = add its Avro schema/mapping and a case here.
/// </summary>
internal static class IntegrationEventAvroMapper
{
    public static GenericRecord ToRecord(IntegrationEvent integrationEvent) => integrationEvent switch
    {
        ShipmentDelayedIntegrationEvent e => ShipmentDelayedAvro.ToRecord(e),
        _ => throw new NotSupportedException($"No Avro schema registered for {integrationEvent.EventType}.")
    };
}
