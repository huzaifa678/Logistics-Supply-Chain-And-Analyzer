using Logistics.Domain.Entities;

namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction for routes. Implemented in Infrastructure with Cypher —
/// the Application layer never sees Neo4j types.
/// </summary>
public interface IRouteRepository
{
    Task<string> AddAsync(Route route, CancellationToken ct = default);
    Task<Route?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Route>> ListAsync(CancellationToken ct = default);
    Task<bool> WarehouseExistsAsync(string warehouseId, CancellationToken ct = default);
}
