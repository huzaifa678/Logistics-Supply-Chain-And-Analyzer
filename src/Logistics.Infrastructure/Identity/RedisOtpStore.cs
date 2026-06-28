using Logistics.Application.Identity;
using StackExchange.Redis;

namespace Logistics.Infrastructure.Identity;

/// <summary>
/// Redis-backed login-OTP store. Codes are stored hashed under a per-email key with a native TTL,
/// so they expire on their own. Verification is an atomic compare-and-delete (Lua), making a
/// correct code strictly single-use even across many API instances.
/// </summary>
public sealed class RedisOtpStore(IConnectionMultiplexer redis) : IOtpStore
{
    // KEYS[1] = otp key, ARGV[1] = candidate hash. Deletes and returns 1 only on an exact match.
    private const string CompareAndDeleteScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            redis.call('DEL', KEYS[1])
            return 1
        end
        return 0
        """;

    private static string Key(string email) => $"otp:login:{email}";

    public Task StoreAsync(string email, string codeHash, TimeSpan ttl, CancellationToken ct = default) =>
        redis.GetDatabase().StringSetAsync(Key(email), codeHash, ttl);

    public async Task<bool> ConsumeAsync(string email, string codeHash, CancellationToken ct = default)
    {
        var result = await redis.GetDatabase().ScriptEvaluateAsync(
            CompareAndDeleteScript,
            new RedisKey[] { Key(email) },
            new RedisValue[] { codeHash });
        return (int)result == 1;
    }
}
