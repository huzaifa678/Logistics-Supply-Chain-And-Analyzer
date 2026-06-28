using System.Collections.Concurrent;
using Logistics.Application.Identity;

namespace Logistics.Infrastructure.Identity;

/// <summary>
/// Process-local OTP store used when Redis isn't configured (single-instance / dev). Codes are
/// stored hashed with an absolute expiry and consumed single-use. Not shared across instances —
/// for a multi-instance deployment, enable Redis so <see cref="RedisOtpStore"/> is used instead.
/// </summary>
public sealed class InMemoryOtpStore : IOtpStore
{
    private readonly ConcurrentDictionary<string, (string Hash, DateTimeOffset ExpiresAt)> _codes = new();

    public Task StoreAsync(string email, string codeHash, TimeSpan ttl, CancellationToken ct = default)
    {
        _codes[email] = (codeHash, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<bool> ConsumeAsync(string email, string codeHash, CancellationToken ct = default)
    {
        if (_codes.TryGetValue(email, out var entry)
            && entry.ExpiresAt > DateTimeOffset.UtcNow
            && entry.Hash == codeHash)
        {
            _codes.TryRemove(email, out _);
            return Task.FromResult(true);
        }

        // Drop an expired entry opportunistically.
        if (_codes.TryGetValue(email, out var stale) && stale.ExpiresAt <= DateTimeOffset.UtcNow)
            _codes.TryRemove(email, out _);

        return Task.FromResult(false);
    }
}
