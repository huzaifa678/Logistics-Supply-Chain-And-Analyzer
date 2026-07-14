using System.Security.Cryptography;
using Logistics.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace Logistics.Application.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ISecureTokenGenerator tokenGenerator,
    IOtpStore otpStore,
    IOtpSender otpSender,
    ITokenIssuer tokenIssuer,
    IOptions<OtpSettings> otpOptions) : IRequestHandler<LoginCommand, Result<LoginResult>>
{
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
            return Result<LoginResult>.Success(new LoginResult(OtpRequired: false, await tokenIssuer.IssueAsync(user, ct)));

        // OTP enabled: generate + store a single-use code and deliver it. No tokens yet.
        var code = GenerateNumericCode(_otp.Length);
        await otpStore.StoreAsync(email, tokenGenerator.Hash(code), _otp.Ttl, ct);
        await otpSender.SendAsync(user, code, ct);

        return Result<LoginResult>.Success(new LoginResult(OtpRequired: true, Tokens: null));
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
