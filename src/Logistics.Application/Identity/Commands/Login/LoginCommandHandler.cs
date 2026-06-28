using System.Security.Cryptography;
using Logistics.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Options;
using DomainRefreshToken = Logistics.Domain.Identity.RefreshToken;

namespace Logistics.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwt,
    ISecureTokenGenerator tokenGenerator,
    IOtpStore otpStore,
    IOtpSender otpSender,
    IOptions<AuthSettings> authOptions,
    IOptions<OtpSettings> otpOptions) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly AuthSettings _auth = authOptions.Value;
    private readonly OtpSettings _otp = otpOptions.Value;

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);

        // Same generic message whether the user is missing or the password is wrong.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResult>.Failure("Invalid email or password.");

        // OTP disabled (dev/test): issue tokens straight away.
        if (!_otp.Enabled)
            return Result<LoginResult>.Success(new LoginResult(OtpRequired: false, await IssueTokensAsync(user, ct)));

        // OTP enabled: generate + store a single-use code and deliver it. No tokens yet.
        var code = GenerateNumericCode(_otp.Length);
        await otpStore.StoreAsync(email, tokenGenerator.Hash(code), _otp.Ttl, ct);
        await otpSender.SendAsync(user, code, ct);

        return Result<LoginResult>.Success(new LoginResult(OtpRequired: true, Tokens: null));
    }

    private async Task<AuthResult> IssueTokensAsync(Domain.Identity.User user, CancellationToken ct)
    {
        var access = jwt.Generate(user);

        // Issue + persist a hashed refresh token; hand the raw value back to the client.
        var rawRefresh = tokenGenerator.GenerateRawToken();
        var refresh = DomainRefreshToken.Issue(user.Id, tokenGenerator.Hash(rawRefresh), _auth.RefreshTokenLifetime);
        await refreshTokens.AddAsync(refresh, ct);

        return new AuthResult(access.Value, access.ExpiresAtUtc, rawRefresh);
    }

    private static string GenerateNumericCode(int length)
    {
        // Uniform, unbiased digits from a CSPRNG.
        var digits = new char[length];
        for (var i = 0; i < length; i++)
            digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(digits);
    }
}
