using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    ISecureTokenGenerator tokenGenerator) : IRequestHandler<RevokeTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var existing = await refreshTokens.GetByHashAsync(
            tokenGenerator.Hash(request.RefreshToken), ct);

        // Idempotent: revoking an unknown/already-revoked token still "succeeds".
        if (existing is { IsActive: true })
        {
            existing.Revoke();
            await refreshTokens.UpdateAsync(existing, ct);
        }

        return Result.Success();
    }
}
