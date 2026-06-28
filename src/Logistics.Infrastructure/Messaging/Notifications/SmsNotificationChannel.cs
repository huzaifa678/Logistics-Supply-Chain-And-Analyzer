using System.Net.Http.Headers;
using System.Text;
using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Notifications;

/// <summary>
/// SMS delivery via a Twilio-compatible REST API. The injected HttpClient carries the standard
/// resilience pipeline (retry + circuit breaker). Logs (no send) when disabled.
/// </summary>
public sealed class SmsNotificationChannel(
    HttpClient httpClient,
    IOptions<SmsSettings> options,
    ILogger<SmsNotificationChannel> logger) : INotificationChannel
{
    private readonly SmsSettings _settings = options.Value;

    public string Channel => "sms";

    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("SMS disabled; would send to {Recipient}: {Subject}",
                message.Recipient, message.Subject);
            return;
        }

        var url = $"{_settings.ApiBaseUrl}/Accounts/{_settings.AccountSid}/Messages.json";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = message.Recipient,
                ["From"] = _settings.FromNumber,
                ["Body"] = $"{message.Subject}: {message.Body}",
            }),
        };
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"SMS provider returned {(int)response.StatusCode}: {body}");
        }

        logger.LogInformation("SMS sent to {Recipient}", message.Recipient);
    }
}
