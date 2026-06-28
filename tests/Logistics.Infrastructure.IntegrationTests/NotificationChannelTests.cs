using System.Net;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Pure unit tests (no Docker) for the email/SMS delivery channels.</summary>
public class NotificationChannelTests
{
    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status);
        }
    }

    private static readonly NotificationMessage Msg = new("sms", "+15551234567", "Delayed", "Shipment s1 delayed");

    [Fact]
    public void Channels_ExposeTheirNames()
    {
        var email = new EmailNotificationChannel(
            Options.Create(new EmailSettings()), NullLogger<EmailNotificationChannel>.Instance);
        var sms = new SmsNotificationChannel(
            new HttpClient(new StubHandler(HttpStatusCode.OK)),
            Options.Create(new SmsSettings()), NullLogger<SmsNotificationChannel>.Instance);

        Assert.Equal("email", email.Channel);
        Assert.Equal("sms", sms.Channel);
    }

    [Fact]
    public async Task EmailChannel_Disabled_DoesNotThrow()
    {
        var channel = new EmailNotificationChannel(
            Options.Create(new EmailSettings { Enabled = false }),
            NullLogger<EmailNotificationChannel>.Instance);

        await channel.SendAsync(new NotificationMessage("email", "ops@x.com", "Hi", "Body"));
    }

    [Fact]
    public async Task SmsChannel_Disabled_DoesNotCallProvider()
    {
        var handler = new StubHandler(HttpStatusCode.Created);
        var channel = new SmsNotificationChannel(
            new HttpClient(handler), Options.Create(new SmsSettings { Enabled = false }),
            NullLogger<SmsNotificationChannel>.Instance);

        await channel.SendAsync(Msg);

        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task SmsChannel_Enabled_PostsFormWithBasicAuth()
    {
        var handler = new StubHandler(HttpStatusCode.Created);
        var channel = new SmsNotificationChannel(
            new HttpClient(handler),
            Options.Create(new SmsSettings
            {
                Enabled = true,
                ApiBaseUrl = "https://sms.test/v1",
                AccountSid = "AC123",
                AuthToken = "tok",
                FromNumber = "+15550000000",
            }),
            NullLogger<SmsNotificationChannel>.Instance);

        await channel.SendAsync(Msg);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://sms.test/v1/Accounts/AC123/Messages.json", handler.Request.RequestUri!.ToString());
        Assert.Equal("Basic", handler.Request.Headers.Authorization!.Scheme);
        Assert.Contains("To=%2B15551234567", handler.Body);
        Assert.Contains("From=%2B15550000000", handler.Body);
    }

    [Fact]
    public async Task SmsChannel_ProviderError_Throws()
    {
        var handler = new StubHandler(HttpStatusCode.BadRequest);
        var channel = new SmsNotificationChannel(
            new HttpClient(handler),
            Options.Create(new SmsSettings { Enabled = true, AccountSid = "AC", AuthToken = "t", FromNumber = "+1" }),
            NullLogger<SmsNotificationChannel>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => channel.SendAsync(Msg));
    }
}
