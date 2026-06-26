namespace Logistics.Infrastructure.Webhooks;

/// <summary>
/// Bound from "Webhooks". Disabled by default — set Webhooks:Enabled + Webhooks:Url (and a Secret
/// for HMAC signing) via env/secret to turn on outbound webhooks.
/// </summary>
public sealed class WebhookSettings
{
    public const string SectionName = "Webhooks";

    public bool Enabled { get; set; } = false;
    public string Url { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 key. When set, each request carries an X-Webhook-Signature header.</summary>
    public string Secret { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
