using Logistics.Domain.Identity;

namespace Logistics.Infrastructure.Persistence.Neo4j.Graph;

/// <summary>
/// ORM node model for (:RefreshToken). Carries <see cref="UserId"/> as a property (in addition
/// to the (:User)-[:HAS_TOKEN]-> relationship) so reads don't need to traverse the edge.
/// </summary>
public sealed class RefreshTokenNode
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? RevokedAt { get; set; }
    public string? ReplacedByTokenId { get; set; }

    public static RefreshTokenNode FromDomain(RefreshToken t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        TokenHash = t.TokenHash,
        ExpiresAt = t.ExpiresAt.ToString("o"),
        CreatedAt = t.CreatedAt.ToString("o"),
        RevokedAt = t.RevokedAt?.ToString("o"),
        ReplacedByTokenId = t.ReplacedByTokenId
    };

    public RefreshToken ToDomain() => RefreshToken.Rehydrate(
        Id, UserId, TokenHash,
        DateTimeOffset.Parse(ExpiresAt),
        DateTimeOffset.Parse(CreatedAt),
        string.IsNullOrEmpty(RevokedAt) ? null : DateTimeOffset.Parse(RevokedAt),
        ReplacedByTokenId);
}
