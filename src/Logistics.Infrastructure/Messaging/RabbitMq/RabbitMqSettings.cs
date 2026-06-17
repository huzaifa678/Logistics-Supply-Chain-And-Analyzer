namespace Logistics.Infrastructure.Messaging.RabbitMq;

/// <summary>Bound from "Messaging:RabbitMq". Disabled by default so dev/tests need no broker.</summary>
public sealed class RabbitMqSettings
{
    public const string SectionName = "Messaging:RabbitMq";

    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Queue { get; set; } = "notifications";
}
