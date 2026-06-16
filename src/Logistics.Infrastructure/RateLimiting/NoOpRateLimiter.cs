using Logistics.Application.Common.Interfaces;

namespace Logistics.Infrastructure.RateLimiting;

/// <summary>Always-allow limiter used when rate limiting is disabled (local dev / tests).</summary>
public sealed class NoOpRateLimiter : IRateLimiter
{
    public Task<RateLimitDecision> AcquireAsync(string partitionKey, CancellationToken ct = default)
        => Task.FromResult(RateLimitDecision.Allow(double.PositiveInfinity));
}
