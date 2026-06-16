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
