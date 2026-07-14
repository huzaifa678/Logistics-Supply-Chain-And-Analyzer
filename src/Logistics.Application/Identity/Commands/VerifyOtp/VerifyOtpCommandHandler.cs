using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Identity.Commands.VerifyOtp;

/// <summary>
/// Second leg of the two-step login: validates the one-time code issued by <c>LoginCommand</c>
/// and, on success, issues the access + refresh token pair.
/// </summary>
public sealed class VerifyOtpCommandHandler(
    IUserRepository users,
    ISecureTokenGenerator tokenGenerator,
    IOtpStore otpStore,
    ITokenIssuer tokenIssuer) : IRequestHandler<VerifyOtpCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Single-use: a correct code is consumed so it can't be replayed.
        if (!await otpStore.ConsumeAsync(email, tokenGenerator.Hash(request.Code), ct))
            return Result<AuthResult>.Failure("Invalid or expired code.");

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null)
            return Result<AuthResult>.Failure("Invalid or expired code.");

        return Result<AuthResult>.Success(await tokenIssuer.IssueAsync(user, ct));
    }
}
