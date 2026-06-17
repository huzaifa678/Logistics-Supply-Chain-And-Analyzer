using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Migrations;

/// <summary>
/// A single, ordered, idempotent graph migration. Neo4j has no built-in migration engine, so
/// the <see cref="GraphMigrationRunner"/> applies these in <see cref="Id"/> order and records
/// each in a (:__Migration) node so it runs exactly once per database.
///
/// Migrations MUST be idempotent (use <c>IF NOT EXISTS</c>, <c>MERGE</c>, or
/// <c>WHERE ... IS NULL</c>) because recording happens in a separate transaction from the body —
/// a crash between the two means the body may re-run on next startup.
///
/// Keep each migration single-concern: schema (constraints/indexes) OR data. Neo4j forbids mixing
/// schema and data writes in one transaction.
/// </summary>
public interface IGraphMigration
{
    /// <summary>Sortable, unique id, e.g. <c>"0001_initial_schema"</c>. Defines apply order.</summary>
    string Id { get; }

    /// <summary>Apply the migration. Use <paramref name="session"/> to run write transactions.</summary>
    Task UpAsync(IAsyncSession session, CancellationToken ct);
}
