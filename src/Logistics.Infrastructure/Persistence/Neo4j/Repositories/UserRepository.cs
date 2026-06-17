using Logistics.Application.Identity;
using Logistics.Domain.Identity;
using Logistics.Infrastructure.Persistence.Neo4j.Graph;
using Neo4jClient;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

/// <summary>
/// User persistence via the Neo4jClient ORM. Queries are built with the typed fluent API and
/// nodes are mapped to/from <see cref="UserNode"/> automatically — no raw Cypher strings, no
/// manual <c>record["x"].As&lt;&gt;()</c> mapping.
/// </summary>
public sealed class UserRepository(Neo4jGraphClientProvider graph) : IUserRepository
{
    private IGraphClient Cypher => graph.Client;

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var counts = await Cypher.Cypher
            .Match("(u:User)")
            .Where((UserNode u) => u.Email == email)
            .Return(u => u.Count())
            .ResultsAsync;

        return counts.Single() > 0;
    }

    public async Task<string> AddAsync(User user, CancellationToken ct = default)
    {
        await Cypher.Cypher
            .Create("(u:User $user)")
            .WithParam("user", UserNode.FromDomain(user))
            .ExecuteWithoutResultsAsync();

        return user.Id;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var nodes = await Cypher.Cypher
            .Match("(u:User)")
            .Where((UserNode u) => u.Email == email)
            .Return(u => u.As<UserNode>())
            .ResultsAsync;

        return nodes.SingleOrDefault()?.ToDomain();
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var nodes = await Cypher.Cypher
            .Match("(u:User)")
            .Where((UserNode u) => u.Id == id)
            .Return(u => u.As<UserNode>())
            .ResultsAsync;

        return nodes.SingleOrDefault()?.ToDomain();
    }
}
