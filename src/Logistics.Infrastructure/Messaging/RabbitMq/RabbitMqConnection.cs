using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Logistics.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Owns a single RabbitMQ <see cref="IConnection"/> for the app lifetime (register as a
/// singleton). Connections are expensive; channels are cheap and created per publisher/consumer.
/// The durable queue is declared once on connect so both ends agree on its shape.
/// </summary>
public sealed class RabbitMqConnection(IOptions<RabbitMqSettings> options) : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private IConnection? _connection;

    public string Queue => _settings.Queue;

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };
        _connection = await factory.CreateConnectionAsync(ct);
        return _connection;
    }

    /// <summary>Open a channel and ensure the durable notifications queue exists.</summary>
    public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.QueueDeclareAsync(
            queue: _settings.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, cancellationToken: ct);
        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
