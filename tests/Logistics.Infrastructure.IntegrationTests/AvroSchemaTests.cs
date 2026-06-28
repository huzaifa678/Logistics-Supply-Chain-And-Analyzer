using Avro.Generic;
using Avro.IO;
using Logistics.Application.Shipments.IntegrationEvents;
using Logistics.Infrastructure.Messaging.Kafka.Serialization;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Validates the Avro schema + mapping by round-tripping through Avro binary encoding directly
/// (no Schema Registry / Kafka needed) — proves the schema matches the event shape.
/// </summary>
public class AvroSchemaTests
{
    [Fact]
    public void ShipmentDelayed_RoundTripsThroughAvroBinary()
    {
        var original = new ShipmentDelayedIntegrationEvent("s1", "TRK-1", "port congestion", "+15551230008");

        var record = ShipmentDelayedAvro.ToRecord(original);

        using var stream = new MemoryStream();
        var writer = new GenericDatumWriter<GenericRecord>(ShipmentDelayedAvro.Schema);
        writer.Write(record, new BinaryEncoder(stream));
        stream.Position = 0;

        var reader = new GenericDatumReader<GenericRecord>(ShipmentDelayedAvro.Schema, ShipmentDelayedAvro.Schema);
        var decoded = reader.Read(null!, new BinaryDecoder(stream));
        var result = ShipmentDelayedAvro.FromRecord(decoded);

        Assert.Equal(original.ShipmentId, result.ShipmentId);
        Assert.Equal(original.TrackingNumber, result.TrackingNumber);
        Assert.Equal(original.Reason, result.Reason);
        Assert.Equal(original.EventId, result.EventId);
    }
}
