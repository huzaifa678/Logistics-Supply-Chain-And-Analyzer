namespace Logistics.Infrastructure.Messaging.Kafka;

/// <summary>Bound from "Messaging:Kafka". Disabled by default so dev/tests need no broker.</summary>
public sealed class KafkaSettings
{
    public const string SectionName = "Messaging:Kafka";

    public bool Enabled { get; set; } = false;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "logistics.events";
    public string ConsumerGroup { get; set; } = "logistics-api";

    /// <summary>Confluent-compatible Schema Registry endpoint (Redpanda serves one on :8081).</summary>
    public string SchemaRegistryUrl { get; set; } = "http://localhost:8081";

    /// <summary>
    /// Dead-letter topic for messages that can't be deserialized or processed. Empty → "&lt;Topic&gt;.DLT".
    /// </summary>
    public string DeadLetterTopic { get; set; } = string.Empty;

    /// <summary>Resolved dead-letter topic name.</summary>
    public string ResolvedDeadLetterTopic =>
        string.IsNullOrWhiteSpace(DeadLetterTopic) ? $"{Topic}.DLT" : DeadLetterTopic;
}
