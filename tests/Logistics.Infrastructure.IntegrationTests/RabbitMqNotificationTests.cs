using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Messaging;
using Logistics.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;
using Testcontainers.RabbitMq;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>Publishes a notification to real RabbitMQ and reads it back off the queue (needs Docker).</summary>
public class RabbitMqNotificationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3.13").Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private RabbitMqConnection NewConnection()
    {
        var uri = new Uri(_container.GetConnectionString());
        var creds = uri.UserInfo.Split(':');
        return new RabbitMqConnection(Options.Create(new RabbitMqSettings
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = creds[0],
            Password = creds[1],
            Queue = "test.notifications"
        }));
    }

    [Fact]
    public async Task PublishAsync_EnqueuesRetrievableNotification()
    {
        await using var connection = NewConnection();
        var publisher = new RabbitMqNotificationPublisher(connection);

        await publisher.PublishAsync(
            new NotificationMessage("email", "ops@example.com", "Delayed", "Shipment s1 delayed"));

        // Pull it straight off the queue and verify the payload round-tripped.
        await using var channel = await connection.CreateChannelAsync();
        var get = await channel.BasicGetAsync(connection.Queue, autoAck: true);

        Assert.NotNull(get);
        var json = Encoding.UTF8.GetString(get!.Body.Span);
        var message = JsonSerializer.Deserialize<NotificationMessage>(json);

        Assert.Equal("ops@example.com", message!.Recipient);
        Assert.Equal("Delayed", message.Subject);
    }
}
