using System.Net;
using System.Net.Mail;
using Logistics.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Messaging.Notifications;

/// <summary>Email delivery over SMTP. Logs (no send) when disabled so dev needs no mail server.</summary>
public sealed class EmailNotificationChannel(
    IOptions<EmailSettings> options,
    ILogger<EmailNotificationChannel> logger) : INotificationChannel
{
    private readonly EmailSettings _settings = options.Value;

    public string Channel => "email";

    public async Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Email disabled; would send to {Recipient}: {Subject}",
                message.Recipient, message.Subject);
            return;
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = string.IsNullOrEmpty(_settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.Username, _settings.Password),
        };
        using var mail = new MailMessage(_settings.FromAddress, message.Recipient, message.Subject, message.Body);

        await client.SendMailAsync(mail, ct);
        logger.LogInformation("Email sent to {Recipient}: {Subject}", message.Recipient, message.Subject);
    }
}
