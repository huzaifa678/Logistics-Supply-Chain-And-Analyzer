using Avro;
using Avro.Generic;
using Logistics.Application.Shipments.IntegrationEvents;

namespace Logistics.Infrastructure.Messaging.Kafka.Serialization;

/// <summary>
/// Avro contract for <see cref="ShipmentDelayedIntegrationEvent"/>. The schema is the source of
/// truth registered in the Schema Registry; this maps between the .NET event and an Avro
/// <see cref="GenericRecord"/> (no codegen, so the schema stays readable and reviewable here).
/// </summary>
public static class ShipmentDelayedAvro
{
    public const string SchemaJson = """
    {
      "type": "record",
      "name": "ShipmentDelayed",
      "namespace": "logistics.events",
      "fields": [
        { "name": "eventId",        "type": "string" },
        { "name": "occurredOn",     "type": "string" },
        { "name": "shipmentId",     "type": "string" },
        { "name": "trackingNumber", "type": "string" },
        { "name": "reason",         "type": "string" },
        { "name": "customerPhone",  "type": ["null", "string"], "default": null }
      ]
    }
    """;

    public static readonly RecordSchema Schema = (RecordSchema)Avro.Schema.Parse(SchemaJson);

    /// <summary>Record name used to route incoming records to this mapping.</summary>
    public const string RecordName = "ShipmentDelayed";

    public static GenericRecord ToRecord(ShipmentDelayedIntegrationEvent e)
    {
        var record = new GenericRecord(Schema);
        record.Add("eventId", e.EventId);
        record.Add("occurredOn", e.OccurredOn.ToString("o"));
        record.Add("shipmentId", e.ShipmentId);
        record.Add("trackingNumber", e.TrackingNumber);
        record.Add("reason", e.Reason);
        record.Add("customerPhone", string.IsNullOrWhiteSpace(e.CustomerPhone) ? null : e.CustomerPhone);
        return record;
    }

    public static ShipmentDelayedIntegrationEvent FromRecord(GenericRecord record) =>
        new(
            ShipmentId: Get(record, "shipmentId"),
            TrackingNumber: Get(record, "trackingNumber"),
            Reason: Get(record, "reason"),
            CustomerPhone: Get(record, "customerPhone"))
        {
            EventId = Get(record, "eventId"),
            OccurredOn = DateTimeOffset.Parse(Get(record, "occurredOn"))
        };

    private static string Get(GenericRecord record, string field) =>
        record.TryGetValue(field, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
}
