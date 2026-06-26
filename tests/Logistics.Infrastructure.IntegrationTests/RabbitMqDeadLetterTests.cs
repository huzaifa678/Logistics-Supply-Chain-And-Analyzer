using System.Text;
using Logistics.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies the dead-letter topology: a message that keeps being nacked is dead-lettered to the
/// DLQ once it exceeds the quorum queue's delivery limit (instead of requeuing forever). Needs Docker.
/// </summary>
public class RabbitMqDeadLetterTests : IAsyncLifetime
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
            Queue = "test.notifications",
            DeadLetterExchange = "test.notifications.dlx",
            DeadLetterQueue = "test.notifications.dlq",
            DeliveryLimit = 1,
        }));
    }

    [Fact]
    public async Task RepeatedlyNackedMessage_LandsInDeadLetterQueue()
    {
        await using var connection = NewConnection();
        await using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes("{\"poison\":true}");
        await channel.BasicPublishAsync(
            exchange: string.Empty, routingKey: "test.notifications",
            mandatory: false, basicProperties: new BasicProperties(), body: body);

        // Consume + nack(requeue) repeatedly; with delivery-limit = 1 the broker dead-letters it.
        string? dlqBody = null;
        for (var attempt = 0; attempt < 20 && dlqBody is null; attempt++)
        {
            var main = await channel.BasicGetAsync("test.notifications", autoAck: false);
            if (main is not null)
                await channel.BasicNackAsync(main.DeliveryTag, multiple: false, requeue: true);

            var dead = await channel.BasicGetAsync("test.notifications.dlq", autoAck: true);
            if (dead is not null)
                dlqBody = Encoding.UTF8.GetString(dead.Body.Span);

            await Task.Delay(250);
        }

        Assert.NotNull(dlqBody);
        Assert.Contains("poison", dlqBody);
    }
}
