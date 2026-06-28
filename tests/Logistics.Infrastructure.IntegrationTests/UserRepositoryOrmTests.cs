using Logistics.Domain.Identity;
using Logistics.Infrastructure.Persistence.Neo4j;
using Logistics.Infrastructure.Persistence.Neo4j.Repositories;
using Microsoft.Extensions.Options;
using Testcontainers.Neo4j;
using Xunit;

namespace Logistics.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies the Neo4jClient ORM repositories against real Neo4j — in particular that the
/// camelCase contract resolver round-trips PascalCase node models to camelCase node properties.
/// </summary>
public class UserRepositoryOrmTests : IAsyncLifetime
{
    private readonly Neo4jContainer _container = new Neo4jBuilder("neo4j:5-community").Build();
    private Neo4jContext _context = null!;
    private Neo4jGraphClientProvider _graph = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _context = new Neo4jContext(Options.Create(new Neo4jSettings
        {
            Uri = _container.GetConnectionString(),
            Username = "neo4j",
            Password = "neo4j"
        }));
        _graph = new Neo4jGraphClientProvider(_context);
        await _graph.ConnectAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByEmailAndId_RoundTrips()
    {
        var repo = new UserRepository(_graph);
        var user = User.Create("alice@example.com", "hashed-pw", "Alice", "+15551230011", Role.Operator);

        var id = await repo.AddAsync(user);

        var byEmail = await repo.GetByEmailAsync("alice@example.com");
        var byId = await repo.GetByIdAsync(id);

        Assert.NotNull(byEmail);
        Assert.Equal("alice@example.com", byEmail!.Email);
        Assert.Equal(Role.Operator, byEmail.Role);
        Assert.Equal("hashed-pw", byEmail.PasswordHash);

        Assert.NotNull(byId);
        Assert.Equal(id, byId!.Id);
    }

    [Fact]
    public async Task EmailExistsAsync_ReflectsState()
    {
        var repo = new UserRepository(_graph);
        Assert.False(await repo.EmailExistsAsync("bob@example.com"));

        await repo.AddAsync(User.Create("bob@example.com", "pw", "Bob"));

        Assert.True(await repo.EmailExistsAsync("bob@example.com"));
    }

    [Fact]
    public async Task RefreshToken_AddGetUpdate_RoundTrips()
    {
        var users = new UserRepository(_graph);
        var tokens = new RefreshTokenRepository(_graph);
        var user = User.Create("carol@example.com", "pw", "Carol");
        await users.AddAsync(user);

        var token = RefreshToken.Issue(user.Id, "hash-1", TimeSpan.FromDays(7));
        await tokens.AddAsync(token);

        var loaded = await tokens.GetByHashAsync("hash-1");
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded!.UserId);
        Assert.True(loaded.IsActive);

        loaded.Revoke(replacedByTokenId: "next-token");
        await tokens.UpdateAsync(loaded);

        var afterRevoke = await tokens.GetByHashAsync("hash-1");
        Assert.False(afterRevoke!.IsActive);
        Assert.Equal("next-token", afterRevoke.ReplacedByTokenId);
    }
}
