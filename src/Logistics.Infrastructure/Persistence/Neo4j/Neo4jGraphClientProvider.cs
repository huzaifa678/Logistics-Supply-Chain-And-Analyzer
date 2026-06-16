using Neo4jClient;
using Newtonsoft.Json.Serialization;

namespace Logistics.Infrastructure.Persistence.Neo4j;

/// <summary>
/// Provides the Neo4jClient ORM (<see cref="IGraphClient"/>) used by the CRUD repositories.
/// It wraps the <b>same</b> <see cref="Neo4jContext.Driver"/> instance, so the ORM and the
/// raw-driver analytics/streaming paths share one connection pool. Register as a singleton.
///
/// The camel-case contract resolver makes PascalCase C# node models map to the camelCase
/// property names stored in Neo4j, so no per-property attributes are needed.
/// </summary>
public sealed class Neo4jGraphClientProvider
{
    private readonly BoltGraphClient _client;

    public Neo4jGraphClientProvider(Neo4jContext context)
    {
        _client = new BoltGraphClient(context.Driver, useDriverDateTypes: false)
        {
            JsonContractResolver = new CamelCasePropertyNamesContractResolver()
        };
    }

    public IGraphClient Client => _client;

    /// <summary>Connect once at startup (idempotent). The wrapped driver is disposed by Neo4jContext.</summary>
    public Task ConnectAsync() => _client.IsConnected ? Task.CompletedTask : _client.ConnectAsync();
}
