namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Distributed rate limiter. The implementation is responsible for being atomic and
/// consistent across all API instances (e.g. Redis-backed). Callers don't know the algorithm.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempt to consume a single token from <paramref name="partitionKey"/>'s bucket.
    /// </summary>
    Task<RateLimitDecision> AcquireAsync(string partitionKey, CancellationToken ct = default);
}

/// <summary>Outcome of a rate-limit check.</summary>
/// <param name="Allowed">Whether the request may proceed.</param>
/// <param name="TokensRemaining">Tokens left in the bucket after this attempt.</param>
/// <param name="RetryAfter">When denied, how long until at least one token is available.</param>
public readonly record struct RateLimitDecision(bool Allowed, double TokensRemaining, TimeSpan RetryAfter)
{
    public static RateLimitDecision Allow(double remaining) => new(true, remaining, TimeSpan.Zero);
}
