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
    IOptions<AuthSettings> authOptions) : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly AuthSettings _auth = authOptions.Value;

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);

        // Same generic message whether the user is missing or the password is wrong.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResult>.Failure("Invalid email or password.");

        var access = jwt.Generate(user);

        // Issue + persist a hashed refresh token; hand the raw value back to the client.
        var rawRefresh = tokenGenerator.GenerateRawToken();
        var refresh = DomainRefreshToken.Issue(user.Id, tokenGenerator.Hash(rawRefresh), _auth.RefreshTokenLifetime);
        await refreshTokens.AddAsync(refresh, ct);

        return Result<AuthResult>.Success(
            new AuthResult(access.Value, access.ExpiresAtUtc, rawRefresh));
    }
}
