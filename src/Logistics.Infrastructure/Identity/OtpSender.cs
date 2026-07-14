using Logistics.Application.Common.Messaging;
using Logistics.Application.Identity;
using Logistics.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Logistics.Infrastructure.Identity;

/// <summary>
/// Delivers a login OTP over the notification channels directly (not via the message bus), so the
/// code is sent inline during the login request rather than depending on a broker round-trip.
/// Always emails the user; also SMSes them when a phone number is on file.
/// </summary>
public sealed class OtpSender(
    IEnumerable<INotificationChannel> channels,
    ILogger<OtpSender> logger) : IOtpSender
{
    private const string Subject = "Your login code";

    public async Task SendAsync(User user, string code, CancellationToken ct = default)
    {
        var body = $"Your one-time login code is {code}. It expires shortly. If you didn't try to sign in, ignore this message.";

        // Email is the primary OTP channel: if it can't be delivered the user has no code, so let
        // that failure propagate and fail the login.
        await DispatchAsync("email", user.Email, body, ct);

        // SMS is supplementary. A provider failure (bad number, timeout, or an open circuit breaker
        // when Twilio is down) must not block a login whose code already went out by email — log and
        // move on. Cancellation still propagates.
        if (!string.IsNullOrWhiteSpace(user.Phone))
        {
            try
            {
                await DispatchAsync("sms", user.Phone, body, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "SMS OTP delivery to {Recipient} failed; the code was still emailed.",
                    user.Phone);
            }
        }
    }

    private async Task DispatchAsync(string channelName, string recipient, string body, CancellationToken ct)
    {
        var channel = channels.FirstOrDefault(c => c.Channel == channelName);
        if (channel is null)
        {
            logger.LogWarning("No '{Channel}' notification channel registered; OTP not sent to {Recipient}.",
                channelName, recipient);
            return;
        }

        await channel.SendAsync(new NotificationMessage(channelName, recipient, Subject, body), ct);
    }
}
