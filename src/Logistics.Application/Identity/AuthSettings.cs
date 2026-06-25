namespace Logistics.Application.Identity;

/// <summary>
/// Bound from the "Auth" configuration section. Shared by the JWT generator (Infrastructure)
/// and the auth handlers (Application).
/// </summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "logistics-api";
    public string Audience { get; set; } = "logistics-clients";

    /// <summary>HMAC signing key. MUST be overridden via secrets/env in production.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>
    /// One-time admin-bootstrap secret. When non-empty, POST /api/auth/claim-admin accepts it to
    /// promote a user to Admin — but only while NO administrator exists yet. Empty = disabled.
    /// Set via env/secret (Auth__BootstrapSecret); never commit a real value.
    /// </summary>
    public string BootstrapSecret { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
}
