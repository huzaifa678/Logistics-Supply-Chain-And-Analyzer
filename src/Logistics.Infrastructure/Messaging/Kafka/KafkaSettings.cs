namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>Bound from "Messaging:Kafka". Disabled by default so dev/tests need no broker.</summary>
public sealed class KafkaSettings
{
    public const string SectionName = "Messaging:Kafka";

    public bool Enabled { get; set; } = false;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "logistics.events";
    public string ConsumerGroup { get; set; } = "logistics-api";
}
