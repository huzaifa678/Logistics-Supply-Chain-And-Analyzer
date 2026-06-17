using Logistics.Application.Identity;
using Logistics.Domain.Identity;
using Logistics.Infrastructure.Persistence.Neo4j.Graph;
using Neo4jClient;

namespace Logistics.Infrastructure.Persistence.Neo4j.Repositories;

/// <summary>Refresh-token persistence via the Neo4jClient ORM.</summary>
public sealed class RefreshTokenRepository(Neo4jGraphClientProvider graph) : IRefreshTokenRepository
{
    private IGraphClient Cypher => graph.Client;

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        // Attach the token to its owning user and store userId on the node for cheap reads.
        await Cypher.Cypher
            .Match("(u:User)")
            .Where((UserNode u) => u.Id == token.UserId)
            .Create("(u)-[:HAS_TOKEN]->(t:RefreshToken $token)")
            .WithParam("token", RefreshTokenNode.FromDomain(token))
            .ExecuteWithoutResultsAsync();
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var nodes = await Cypher.Cypher
            .Match("(t:RefreshToken)")
            .Where((RefreshTokenNode t) => t.TokenHash == tokenHash)
            .Return(t => t.As<RefreshTokenNode>())
            .ResultsAsync;

        return nodes.SingleOrDefault()?.ToDomain();
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        await Cypher.Cypher
            .Match("(t:RefreshToken)")
            .Where((RefreshTokenNode t) => t.Id == token.Id)
            .Set("t.revokedAt = $revokedAt, t.replacedByTokenId = $replacedByTokenId")
            .WithParam("revokedAt", token.RevokedAt?.ToString("o"))
            .WithParam("replacedByTokenId", token.ReplacedByTokenId)
            .ExecuteWithoutResultsAsync();
    }
}
