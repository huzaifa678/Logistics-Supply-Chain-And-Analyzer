using Logistics.Infrastructure.RateLimiting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Exercises the Lua token-bucket script against a real Redis (Testcontainers, needs Docker).
/// </summary>
public class RedisTokenBucketRateLimiterTests : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine").Build();
    private IConnectionMultiplexer _redis = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _redis = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
        await _container.DisposeAsync();
    }

    private RedisTokenBucketRateLimiter NewLimiter(int capacity, double refillPerSecond)
        => new(_redis, Options.Create(new RateLimitSettings
        {
            Capacity = capacity,
            RefillTokensPerSecond = refillPerSecond,
            KeyPrefix = "test:"
        }));

    [Fact]
    public async Task Acquire_AllowsUpToCapacity_ThenDenies()
    {
        var limiter = NewLimiter(capacity: 3, refillPerSecond: 0.001); // refill effectively off
        var key = Guid.NewGuid().ToString("N");

        Assert.True((await limiter.AcquireAsync(key)).Allowed);
        Assert.True((await limiter.AcquireAsync(key)).Allowed);
        Assert.True((await limiter.AcquireAsync(key)).Allowed);

        var denied = await limiter.AcquireAsync(key);
        Assert.False(denied.Allowed);
        Assert.True(denied.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task Acquire_RefillsOverTime()
    {
        var limiter = NewLimiter(capacity: 1, refillPerSecond: 50); // ~20ms per token
        var key = Guid.NewGuid().ToString("N");

        Assert.True((await limiter.AcquireAsync(key)).Allowed);   // spend the only token
        Assert.False((await limiter.AcquireAsync(key)).Allowed);  // empty

        await Task.Delay(100); // enough time to refill

        Assert.True((await limiter.AcquireAsync(key)).Allowed);   // refilled
    }

    [Fact]
    public async Task Acquire_IsolatesPartitions()
    {
        var limiter = NewLimiter(capacity: 1, refillPerSecond: 0.001);

        Assert.True((await limiter.AcquireAsync("client-a")).Allowed);
        Assert.True((await limiter.AcquireAsync("client-b")).Allowed); // separate bucket
        Assert.False((await limiter.AcquireAsync("client-a")).Allowed);
    }
}
