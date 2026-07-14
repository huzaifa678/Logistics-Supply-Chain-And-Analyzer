using Logistics.Domain.Identity;
using Microsoft.Extensions.Options;

namespace Logistics.Application.Identity;

/// <summary>
/// Default <see cref="ITokenIssuer"/>: mints a JWT access token and a single hashed refresh token,
/// persists the refresh token, and returns the pair. The raw refresh value is handed back to the
/// caller but never stored. Shared by login and OTP verification so the issuance policy is defined
/// once rather than duplicated per handler.
/// </summary>
public sealed class TokenIssuer(
    IJwtTokenGenerator jwt,
    ISecureTokenGenerator tokenGenerator,
    IRefreshTokenRepository refreshTokens,
    IOptions<AuthSettings> authOptions) : ITokenIssuer
{
    private readonly AuthSettings _auth = authOptions.Value;

    public async Task<AuthResult> IssueAsync(User user, CancellationToken ct = default)
    {
        var access = jwt.Generate(user);

        // Issue + persist a hashed refresh token; hand the raw value back to the client.
        var rawRefresh = tokenGenerator.GenerateRawToken();
        var refresh = RefreshToken.Issue(user.Id, tokenGenerator.Hash(rawRefresh), _auth.RefreshTokenLifetime);
        await refreshTokens.AddAsync(refresh, ct);

        return new AuthResult(access.Value, access.ExpiresAtUtc, rawRefresh);
    }
}
