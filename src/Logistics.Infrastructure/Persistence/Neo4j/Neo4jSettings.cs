namespace Logistics.Infrastructure.Persistence.Neo4j;

/// <summary>Bound from the "Neo4j" configuration section.</summary>
public sealed class Neo4jSettings
{
    public const string SectionName = "Neo4j";

    public string Uri { get; set; } = "bolt://localhost:7687";
    public string Username { get; set; } = "neo4j";
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "neo4j";

    // --- Connection pool tuning ---

    /// <summary>Max pooled connections. Size to your concurrency, not your thread count.</summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>How long to wait for a free connection before failing fast.</summary>
    public int ConnectionAcquisitionTimeoutSeconds { get; set; } = 60;

    /// <summary>TCP connect timeout when opening a new connection.</summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>Recycle connections older than this so load balancers/restarts are honored.</summary>
    public int MaxConnectionLifetimeHours { get; set; } = 1;
}
