namespace Logistics.Application.Common.Webhooks;

/// <summary>An outbound webhook: an event type plus its payload (serialized to JSON for delivery).</summary>
public sealed record WebhookEvent(string EventType, object Data);

/// <summary>
/// Port for delivering webhooks to an external subscriber. Implemented in Infrastructure with a
/// resilient (retry + circuit breaker), signed HTTP client. A no-op when webhooks are disabled.
/// </summary>
public interface IWebhookSender
{
    Task SendAsync(WebhookEvent webhookEvent, CancellationToken ct = default);
}
