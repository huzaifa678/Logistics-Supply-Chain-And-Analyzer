using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Webhooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Webhooks;

/// <summary>
/// Delivers webhooks over HTTP. The injected <see cref="HttpClient"/> is configured with the
/// standard resilience pipeline (retry + circuit breaker + timeout) in DI — this is the one place
/// in the app that calls an external, uncontrolled endpoint, so that's where resilience belongs.
/// Each request is HMAC-SHA256 signed so the receiver can verify authenticity.
/// </summary>
public sealed class HttpWebhookSender(
    HttpClient httpClient,
    IOptions<WebhookSettings> options,
    ILogger<HttpWebhookSender> logger) : IWebhookSender
{
    private readonly WebhookSettings _settings = options.Value;

    public async Task SendAsync(WebhookEvent webhookEvent, CancellationToken ct = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.Url))
            return; // webhooks disabled — no-op

        var envelope = new
        {
            id = Guid.NewGuid().ToString("N"),
            type = webhookEvent.EventType,
            occurredOn = DateTimeOffset.UtcNow,
            data = webhookEvent.Data,
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Url)
        {
            Content = new ByteArrayContent(body),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.TryAddWithoutValidation("X-Webhook-Event", webhookEvent.EventType);
        if (!string.IsNullOrEmpty(_settings.Secret))
            request.Headers.TryAddWithoutValidation("X-Webhook-Signature", Sign(body, _settings.Secret));

        try
        {
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Webhook {Type} returned {Status}", webhookEvent.EventType, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            // The resilience pipeline already retried / opened the circuit. Swallow here so a bad
            // subscriber never breaks the domain-event pipeline.
            logger.LogError(ex, "Webhook {Type} delivery failed", webhookEvent.EventType);
        }
    }

    private static string Sign(byte[] body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();
    }
}
