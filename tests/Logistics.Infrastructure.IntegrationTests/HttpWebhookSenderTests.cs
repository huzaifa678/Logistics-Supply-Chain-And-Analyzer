using System.Net;
using System.Security.Cryptography;
using System.Text;
using Logistics.Application.Common.Webhooks;
using Logistics.Infrastructure.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Pure unit test (no Docker): drives HttpWebhookSender through a stub HttpMessageHandler.</summary>
public class HttpWebhookSenderTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public byte[]? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static HttpWebhookSender Sender(CapturingHandler handler, WebhookSettings settings) =>
        new(new HttpClient(handler), Options.Create(settings), NullLogger<HttpWebhookSender>.Instance);

    [Fact]
    public async Task SendAsync_WhenDisabled_DoesNotCallEndpoint()
    {
        var handler = new CapturingHandler();
        var sender = Sender(handler, new WebhookSettings { Enabled = false, Url = "https://example.test/hook" });

        await sender.SendAsync(new WebhookEvent("warehouse.created", new { id = "w1" }));

        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task SendAsync_WhenEnabled_PostsHmacSignedPayload()
    {
        const string secret = "top-secret";
        var handler = new CapturingHandler();
        var sender = Sender(handler, new WebhookSettings
        {
            Enabled = true,
            Url = "https://example.test/hook",
            Secret = secret,
        });

        await sender.SendAsync(new WebhookEvent("warehouse.created", new { id = "w1" }));

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("warehouse.created", handler.Request.Headers.GetValues("X-Webhook-Event").Single());

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = "sha256=" + Convert.ToHexString(hmac.ComputeHash(handler.Body!)).ToLowerInvariant();
        Assert.Equal(expected, handler.Request.Headers.GetValues("X-Webhook-Signature").Single());

        Assert.Contains("warehouse.created", Encoding.UTF8.GetString(handler.Body!));
    }
}
