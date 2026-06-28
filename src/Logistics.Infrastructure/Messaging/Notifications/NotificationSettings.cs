namespace Logistics.Infrastructure.Messaging.Notifications;

/// <summary>
/// Bound from "Notifications". Recipients the integration-event bridge sends to when it turns a
/// domain event (e.g. a shipment delay) into email + SMS notifications.
/// </summary>
public sealed class NotificationSettings
{
    public const string SectionName = "Notifications";

    public string OpsEmail { get; set; } = "ops@logistics.example";
    public string OpsPhone { get; set; } = string.Empty;
}

/// <summary>Bound from "Notifications:Email". SMTP delivery; log-only when disabled.</summary>
public sealed class EmailSettings
{
    public const string SectionName = "Notifications:Email";

    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@logistics.example";
}

/// <summary>Bound from "Notifications:Sms". Twilio-compatible REST delivery; log-only when disabled.</summary>
public sealed class SmsSettings
{
    public const string SectionName = "Notifications:Sms";

    public bool Enabled { get; set; } = false;
    public string ApiBaseUrl { get; set; } = "https://api.twilio.com/2010-04-01";
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
