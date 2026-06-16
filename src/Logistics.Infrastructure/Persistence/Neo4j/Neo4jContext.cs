using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j;

/// <summary>
/// Owns the single, thread-safe <see cref="IDriver"/> for the application's lifetime
/// (register as a singleton). Hands out short-lived sessions per unit of work.
/// </summary>
public sealed class Neo4jContext : IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly string _database;

    public Neo4jContext(IOptions<Neo4jSettings> options)
    {
        var settings = options.Value;
        _driver = GraphDatabase.Driver(
            settings.Uri,
            AuthTokens.Basic(settings.Username, settings.Password),
            config =>
            {
                // Connection pooling — the driver multiplexes sessions over this pool.
                config.WithMaxConnectionPoolSize(settings.MaxConnectionPoolSize);
                config.WithConnectionAcquisitionTimeout(
                    TimeSpan.FromSeconds(settings.ConnectionAcquisitionTimeoutSeconds));
                config.WithConnectionTimeout(
                    TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds));
                config.WithMaxConnectionLifetime(
                    TimeSpan.FromHours(settings.MaxConnectionLifetimeHours));
            });
        _database = settings.Database;
    }

    /// <summary>
    /// The shared driver. Exposed so the Neo4jClient ORM can wrap the very same driver instance
    /// (one connection pool for both the ORM and the raw-driver paths). Disposal stays here.
    /// </summary>
    public IDriver Driver => _driver;

    /// <summary>Create a session scoped to the configured database. Always dispose it.</summary>
    public IAsyncSession Session(AccessMode mode = AccessMode.Write) =>
        _driver.AsyncSession(o => o
            .WithDatabase(_database)
            .WithDefaultAccessMode(mode));

    public Task VerifyConnectivityAsync() => _driver.VerifyConnectivityAsync();

    public async ValueTask DisposeAsync() => await _driver.DisposeAsync();
}
