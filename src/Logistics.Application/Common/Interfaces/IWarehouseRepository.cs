using Logistics.Domain.Entities;

namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction for warehouses (graph nodes). Implemented in Infrastructure with
/// Cypher — the Application layer never sees Neo4j types.
/// </summary>
public interface IWarehouseRepository
{
    Task<string> AddAsync(Warehouse warehouse, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}
