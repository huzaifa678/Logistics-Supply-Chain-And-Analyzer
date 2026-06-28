using Logistics.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Options;
using DomainRefreshToken = Logistics.Domain.Identity.RefreshToken;

namespace Logistics.Application.Identity.Commands.VerifyOtp;

/// <summary>
/// Second leg of the two-step login: validates the one-time code issued by <c>LoginCommand</c>
/// and, on success, issues the access + refresh token pair.
/// </summary>
public sealed class VerifyOtpCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IJwtTokenGenerator jwt,
    ISecureTokenGenerator tokenGenerator,
    IOtpStore otpStore,
    IOptions<AuthSettings> authOptions) : IRequestHandler<VerifyOtpCommand, Result<AuthResult>>
{
    private readonly AuthSettings _auth = authOptions.Value;

    public async Task<Result<AuthResult>> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Single-use: a correct code is consumed so it can't be replayed.
        if (!await otpStore.ConsumeAsync(email, tokenGenerator.Hash(request.Code), ct))
            return Result<AuthResult>.Failure("Invalid or expired code.");

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null)
            return Result<AuthResult>.Failure("Invalid or expired code.");

        var access = jwt.Generate(user);

        var rawRefresh = tokenGenerator.GenerateRawToken();
        var refresh = DomainRefreshToken.Issue(user.Id, tokenGenerator.Hash(rawRefresh), _auth.RefreshTokenLifetime);
        await refreshTokens.AddAsync(refresh, ct);

        return Result<AuthResult>.Success(new AuthResult(access.Value, access.ExpiresAtUtc, rawRefresh));
    }
}
