using Logistics.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Options;
using DomainRefreshToken = Logistics.Domain.Identity.RefreshToken;

namespace Logistics.Application.Identity.Commands.RefreshToken;

/// <summary>
/// Rotating refresh: the presented token is validated, revoked, and replaced with a new
/// one (revoke-on-use). This limits the blast radius if a refresh token leaks.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IJwtTokenGenerator jwt,
    ISecureTokenGenerator tokenGenerator,
    IOptions<AuthSettings> authOptions) : IRequestHandler<RefreshTokenCommand, Result<AuthResult>>
{
    private readonly AuthSettings _auth = authOptions.Value;

    public async Task<Result<AuthResult>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var presentedHash = tokenGenerator.Hash(request.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(presentedHash, ct);

        if (existing is null || !existing.IsActive)
            return Result<AuthResult>.Failure("Invalid or expired refresh token.");

        var user = await users.GetByIdAsync(existing.UserId, ct);
        if (user is null)
            return Result<AuthResult>.Failure("Invalid or expired refresh token.");

        // Mint the replacement first so we can record the rotation link.
        var rawRefresh = tokenGenerator.GenerateRawToken();
        var replacement = DomainRefreshToken.Issue(
            user.Id, tokenGenerator.Hash(rawRefresh), _auth.RefreshTokenLifetime);

        existing.Revoke(replacedByTokenId: replacement.Id);
        await refreshTokens.UpdateAsync(existing, ct);
        await refreshTokens.AddAsync(replacement, ct);

        var access = jwt.Generate(user);
        return Result<AuthResult>.Success(
            new AuthResult(access.Value, access.ExpiresAtUtc, rawRefresh));
    }
}
