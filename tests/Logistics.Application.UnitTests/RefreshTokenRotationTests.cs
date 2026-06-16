using Logistics.Application.Identity;
using Logistics.Application.Identity.Commands.RefreshToken;
using Logistics.Domain.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Logistics.Application.UnitTests;

public class RefreshTokenRotationTests
{
    private sealed class FakeUsers : IUserRepository
    {
        private readonly User _user;
        public FakeUsers(User user) => _user = user;
        public Task<bool> EmailExistsAsync(string e, CancellationToken c = default) => Task.FromResult(true);
        public Task<string> AddAsync(User u, CancellationToken c = default) => Task.FromResult(u.Id);
        public Task<User?> GetByEmailAsync(string e, CancellationToken c = default) => Task.FromResult<User?>(_user);
        public Task<User?> GetByIdAsync(string id, CancellationToken c = default)
            => Task.FromResult<User?>(_user.Id == id ? _user : null);
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
        private int _n;
        public string GenerateRawToken() => $"raw-{++_n}";
        public string Hash(string raw) => $"hash:{raw}";
    }

    private sealed class FakeJwt : IJwtTokenGenerator
    {
        public AccessToken Generate(User user) => new("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRevokesOld()
    {
        var user = User.Create("a@b.com", "hash", "Alice", Role.Operator);
        var refreshRepo = new FakeRefreshTokens();
        var gen = new FakeTokenGen();

        var seeded = RefreshToken.Issue(user.Id, gen.Hash("raw-seed"), TimeSpan.FromDays(7));
        refreshRepo.Store.Add(seeded);

        var options = Options.Create(new AuthSettings { SigningKey = "x", RefreshTokenDays = 7 });
        var handler = new RefreshTokenCommandHandler(
            new FakeUsers(user), refreshRepo, new FakeJwt(), gen, options);

        var result = await handler.Handle(new RefreshTokenCommand("raw-seed"), default);

        Assert.True(result.Succeeded);
        Assert.False(seeded.IsActive);                       // old token revoked
        Assert.NotNull(seeded.ReplacedByTokenId);            // rotation link recorded
        Assert.Equal(2, refreshRepo.Store.Count);            // a replacement was added
        Assert.NotEqual("raw-seed", result.Value!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Fails()
    {
        var user = User.Create("a@b.com", "hash", "Alice");
        var options = Options.Create(new AuthSettings { SigningKey = "x" });
        var handler = new RefreshTokenCommandHandler(
            new FakeUsers(user), new FakeRefreshTokens(), new FakeJwt(), new FakeTokenGen(), options);

        var result = await handler.Handle(new RefreshTokenCommand("does-not-exist"), default);

        Assert.False(result.Succeeded);
    }
}
