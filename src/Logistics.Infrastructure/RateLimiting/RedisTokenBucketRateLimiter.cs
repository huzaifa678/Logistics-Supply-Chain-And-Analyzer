using Logistics.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Logistics.Infrastructure.RateLimiting;

/// <summary>
/// Distributed token-bucket rate limiter backed by Redis.
///
/// The whole "read bucket → refill by elapsed time → try to spend a token → write back"
/// sequence runs as a single Lua script, which Redis executes **atomically**. That's what
/// makes it correct across many API instances hammering the same bucket concurrently —
/// no read-modify-write races, no double-spend.
/// </summary>
public sealed class RedisTokenBucketRateLimiter : IRateLimiter
{
    // KEYS[1] = bucket key
    // ARGV    = capacity, refillPerSecond, nowSeconds, requestedTokens
    // returns { allowed(0|1), tokensRemaining(string), retryAfterSeconds(string) }
    private const string TokenBucketScript = """
        local capacity   = tonumber(ARGV[1])
        local refill     = tonumber(ARGV[2])
        local now        = tonumber(ARGV[3])
        local requested  = tonumber(ARGV[4])

        local data   = redis.call('HMGET', KEYS[1], 'tokens', 'ts')
        local tokens = tonumber(data[1])
        local ts     = tonumber(data[2])

        if tokens == nil then
            tokens = capacity
            ts = now
        end

        local elapsed = math.max(0, now - ts)
        tokens = math.min(capacity, tokens + elapsed * refill)

        local allowed = 0
        local retry_after = 0
        if tokens >= requested then
            tokens = tokens - requested
            allowed = 1
        else
            retry_after = (requested - tokens) / refill
        end

        redis.call('HSET', KEYS[1], 'tokens', tokens, 'ts', now)
        -- expire idle buckets after a full refill window so Redis self-cleans
        redis.call('EXPIRE', KEYS[1], math.ceil(capacity / refill) + 1)

        return { allowed, tostring(tokens), tostring(retry_after) }
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitSettings _settings;

    public RedisTokenBucketRateLimiter(IConnectionMultiplexer redis, IOptions<RateLimitSettings> options)
    {
        _redis = redis;
        _settings = options.Value;
    }

    public async Task<RateLimitDecision> AcquireAsync(string partitionKey, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = _settings.KeyPrefix + partitionKey;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        var raw = (RedisResult[])(await db.ScriptEvaluateAsync(
            TokenBucketScript,
            [key],
            [_settings.Capacity, _settings.RefillTokensPerSecond, now, 1]))!;

        var allowed = (long)raw[0] == 1;
        var remaining = double.Parse((string)raw[1]!);
        var retryAfter = TimeSpan.FromSeconds(double.Parse((string)raw[2]!));

        return new RateLimitDecision(allowed, remaining, retryAfter);
    }
}
