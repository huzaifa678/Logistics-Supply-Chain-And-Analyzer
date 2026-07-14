using Logistics.Application.Identity;
using Logistics.Application.Identity.Commands.Login;
using Logistics.Application.Identity.Commands.VerifyOtp;
using Logistics.Domain.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Logistics.Application.UnitTests;

public class LoginOtpFlowTests
{
    private sealed class FakeUsers(User user) : IUserRepository
    {
        public Task<bool> EmailExistsAsync(string e, CancellationToken c = default) => Task.FromResult(true);
        public Task<string> AddAsync(User u, CancellationToken c = default) => Task.FromResult(u.Id);
        public Task<User?> GetByEmailAsync(string e, CancellationToken c = default)
            => Task.FromResult<User?>(e == user.Email ? user : null);
        public Task<User?> GetByIdAsync(string id, CancellationToken c = default) => Task.FromResult<User?>(user);
        public Task<IReadOnlyList<User>> ListAsync(CancellationToken c = default)
            => Task.FromResult<IReadOnlyList<User>>([user]);
        public Task UpdateRoleAsync(string id, Role role, CancellationToken c = default) => Task.CompletedTask;
    }

    private sealed class FakeRefreshTokens : IRefreshTokenRepository
    {
        public readonly List<RefreshToken> Store = new();
        public Task AddAsync(RefreshToken t, CancellationToken c = default) { Store.Add(t); return Task.CompletedTask; }
        public Task<RefreshToken?> GetByHashAsync(string h, CancellationToken c = default)
            => Task.FromResult(Store.FirstOrDefault(x => x.TokenHash == h));
        public Task UpdateAsync(RefreshToken t, CancellationToken c = default) => Task.CompletedTask;
    }

    private sealed class FakeTokenGen : ISecureTokenGenerator
    {
        public string GenerateRawToken() => "raw-refresh";
        public string Hash(string raw) => $"hash:{raw}";
    }

    private sealed class FakeJwt : IJwtTokenGenerator
    {
        public AccessToken Generate(User user) => new("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }

    // Verifies any password against the literal "correct".
    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"H({password})";
        public bool Verify(string password, string hash) => password == "correct";
    }

    private sealed class FakeOtpStore : IOtpStore
    {
        public string? StoredHash;
        public Task StoreAsync(string email, string codeHash, TimeSpan ttl, CancellationToken c = default)
        { StoredHash = codeHash; return Task.CompletedTask; }
        public Task<bool> ConsumeAsync(string email, string codeHash, CancellationToken c = default)
        {
            if (StoredHash == codeHash) { StoredHash = null; return Task.FromResult(true); }
            return Task.FromResult(false);
        }
    }

    private sealed class FakeOtpSender : IOtpSender
    {
        public string? SentCode;
        public Task SendAsync(User user, string code, CancellationToken c = default)
        { SentCode = code; return Task.CompletedTask; }
    }

    private static User TestUser() =>
        User.Create("a@b.com", "H(correct)", "Alice", "+15551230099", Role.Operator);

    private static (LoginCommandHandler login, VerifyOtpCommandHandler verify, FakeOtpStore store, FakeOtpSender sender, FakeRefreshTokens refreshTokens)
        Build(bool otpEnabled)
    {
        var user = TestUser();
        var users = new FakeUsers(user);
        var refreshTokens = new FakeRefreshTokens();
        var gen = new FakeTokenGen();
        var store = new FakeOtpStore();
        var sender = new FakeOtpSender();
        var auth = Options.Create(new AuthSettings { SigningKey = "x", RefreshTokenDays = 7 });
        var otp = Options.Create(new OtpSettings { Enabled = otpEnabled, Length = 6 });

        // Real TokenIssuer over fakes — the shared issuance path both handlers now delegate to.
        var tokenIssuer = new TokenIssuer(new FakeJwt(), gen, refreshTokens, auth);

        var login = new LoginCommandHandler(
            users, new FakeHasher(), gen, store, sender, tokenIssuer, otp);
        var verify = new VerifyOtpCommandHandler(users, gen, store, tokenIssuer);
        return (login, verify, store, sender, refreshTokens);
    }

    [Fact]
    public async Task Login_WithOtpEnabled_RequiresOtp_AndSendsCode_ButIssuesNoTokens()
    {
        var (login, _, store, sender, refreshTokens) = Build(otpEnabled: true);

        var result = await login.Handle(new LoginCommand("a@b.com", "correct"), default);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.OtpRequired);
        Assert.Null(result.Value.Tokens);                 // no tokens at step 1
        Assert.NotNull(sender.SentCode);                  // a code was delivered
        Assert.Equal($"hash:{sender.SentCode}", store.StoredHash); // stored hashed, not raw
        Assert.Empty(refreshTokens.Store);                // nothing persisted yet
    }

    [Fact]
    public async Task VerifyOtp_WithCorrectCode_IssuesTokens()
    {
        var (login, verify, _, sender, refreshTokens) = Build(otpEnabled: true);
        await login.Handle(new LoginCommand("a@b.com", "correct"), default);

        var result = await verify.Handle(new VerifyOtpCommand("a@b.com", sender.SentCode!), default);

        Assert.True(result.Succeeded);
        Assert.Equal("access-token", result.Value!.AccessToken);
        Assert.Single(refreshTokens.Store);               // refresh token persisted on success
    }

    [Fact]
    public async Task VerifyOtp_WithWrongCode_Fails_AndIssuesNoTokens()
    {
        var (login, verify, _, _, refreshTokens) = Build(otpEnabled: true);
        await login.Handle(new LoginCommand("a@b.com", "correct"), default);

        var result = await verify.Handle(new VerifyOtpCommand("a@b.com", "000000"), default);

        Assert.False(result.Succeeded);
        Assert.Empty(refreshTokens.Store);
    }

    [Fact]
    public async Task VerifyOtp_CodeIsSingleUse()
    {
        var (login, verify, _, sender, _) = Build(otpEnabled: true);
        await login.Handle(new LoginCommand("a@b.com", "correct"), default);

        var first = await verify.Handle(new VerifyOtpCommand("a@b.com", sender.SentCode!), default);
        var second = await verify.Handle(new VerifyOtpCommand("a@b.com", sender.SentCode!), default);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);                   // consumed — can't be replayed
    }

    [Fact]
    public async Task Login_WithWrongPassword_Fails()
    {
        var (login, _, _, sender, _) = Build(otpEnabled: true);

        var result = await login.Handle(new LoginCommand("a@b.com", "wrong"), default);

        Assert.False(result.Succeeded);
        Assert.Null(sender.SentCode);                     // no code sent for bad credentials
    }

    [Fact]
    public async Task Login_WithOtpDisabled_IssuesTokensImmediately()
    {
        var (login, _, _, sender, refreshTokens) = Build(otpEnabled: false);

        var result = await login.Handle(new LoginCommand("a@b.com", "correct"), default);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.OtpRequired);
        Assert.NotNull(result.Value.Tokens);
        Assert.Null(sender.SentCode);                     // OTP path skipped
        Assert.Single(refreshTokens.Store);
    }
}
