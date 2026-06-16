using Logistics.Domain.Common;

namespace Logistics.Domain.Identity;

/// <summary>
/// A single refresh token issued to a user. Stored hashed; rotated on every use.
/// Modeled in Neo4j as (:User)-[:HAS_TOKEN]->(:RefreshToken).
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    public string UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Set when this token is rotated, pointing at its successor (audit trail).</summary>
    public string? ReplacedByTokenId { get; private set; }

    public bool IsActive => RevokedAt is null && !IsExpired;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    private RefreshToken(string userId, string tokenHash, DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static RefreshToken Issue(string userId, string tokenHash, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("Token hash required.", nameof(tokenHash));
        return new RefreshToken(userId, tokenHash, DateTimeOffset.UtcNow.Add(lifetime));
    }

    public void Revoke(string? replacedByTokenId = null)
    {
        if (RevokedAt is not null) return;
        RevokedAt = DateTimeOffset.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
    }

    public static RefreshToken Rehydrate(
        string id, string userId, string tokenHash,
        DateTimeOffset expiresAt, DateTimeOffset createdAt,
        DateTimeOffset? revokedAt, string? replacedByTokenId)
        => new(userId, tokenHash, expiresAt)
        {
            Id = id,
            CreatedAt = createdAt,
            RevokedAt = revokedAt,
            ReplacedByTokenId = replacedByTokenId
        };
}
