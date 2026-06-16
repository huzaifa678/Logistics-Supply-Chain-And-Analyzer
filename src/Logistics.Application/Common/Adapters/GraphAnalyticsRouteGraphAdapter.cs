using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Services;

namespace Logistics.Application.Common.Adapters;

/// <summary>
/// Adapter pattern: adapts the persistence-facing <see cref="IGraphAnalyticsRepository"/>
/// (which speaks in DB query DTOs) to the domain-facing <see cref="IRouteGraph"/> port the
/// routing service depends on. This is the seam between the database and the service —
/// the domain service stays ignorant of repositories, Cypher, and the Neo4j driver, and the
/// repository stays ignorant of domain services.
/// </summary>
public sealed class GraphAnalyticsRouteGraphAdapter(IGraphAnalyticsRepository analytics) : IRouteGraph
{
    public async Task<GraphPath?> FindShortestPathAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        CancellationToken ct = default)
    {
        var result = await analytics.GetShortestPathAsync(originWarehouseId, destinationWarehouseId, ct);

        // Translate the persistence DTO into the domain port's type.
        return result is null
            ? null
            : new GraphPath(result.WarehouseIds, result.TotalDistanceKm, result.Hops);
    }
}
