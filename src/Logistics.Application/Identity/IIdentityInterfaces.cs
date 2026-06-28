using Logistics.Domain.Identity;

namespace Logistics.Application.Identity;

/// <summary>Hashes and verifies user passwords (e.g. PBKDF2 / BCrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Issues signed JWT access tokens.</summary>
public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

/// <summary>Generates cryptographically-random refresh tokens and hashes them for storage.</summary>
public interface ISecureTokenGenerator
{
    /// <summary>A raw, high-entropy token handed to the client (never stored).</summary>
    string GenerateRawToken();

    /// <summary>One-way hash of a raw token — this is what gets persisted/compared.</summary>
    string Hash(string rawToken);
}

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<string> AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>All users, for the admin user-management view.</summary>
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);

    /// <summary>Persists a role change for the given user (RBAC administration).</summary>
    Task UpdateRoleAsync(string id, Role role, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}

/// <summary>A signed access token plus its expiry.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>The full credential pair returned to clients on login/refresh.</summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken);

/// <summary>
/// Outcome of a password login. When OTP is enforced, <see cref="OtpRequired"/> is true and
/// <see cref="Tokens"/> is null — the client must call verify-otp to complete sign-in. When OTP
/// is disabled, tokens are issued immediately.
/// </summary>
public sealed record LoginResult(bool OtpRequired, AuthResult? Tokens);

/// <summary>
/// Short-lived store for one-time login codes, keyed by user email. Codes are stored hashed and
/// expire automatically; a successful <see cref="ConsumeAsync"/> is single-use.
/// </summary>
public interface IOtpStore
{
    Task StoreAsync(string email, string codeHash, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>True iff a matching, unexpired code exists; consumes it (single-use) on success.</summary>
    Task<bool> ConsumeAsync(string email, string codeHash, CancellationToken ct = default);
}

/// <summary>Delivers a login OTP to the user over the configured channel(s) — email and/or SMS.</summary>
public interface IOtpSender
{
    Task SendAsync(User user, string code, CancellationToken ct = default);
}
