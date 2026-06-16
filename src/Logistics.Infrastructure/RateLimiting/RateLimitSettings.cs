namespace Logistics.Infrastructure.RateLimiting;

/// <summary>Bound from the "RateLimiting" configuration section.</summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>When false, a no-op limiter is used (handy for local dev / tests without Redis).</summary>
    public bool Enabled { get; set; } = false;

    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>Key prefix so buckets don't collide with other apps sharing the Redis instance.</summary>
    public string KeyPrefix { get; set; } = "ratelimit:logistics:";

    /// <summary>Bucket size — the maximum burst a client can make.</summary>
    public int Capacity { get; set; } = 100;

    /// <summary>Sustained refill rate. Steady-state throughput per client = this value.</summary>
    public double RefillTokensPerSecond { get; set; } = 10;
}
