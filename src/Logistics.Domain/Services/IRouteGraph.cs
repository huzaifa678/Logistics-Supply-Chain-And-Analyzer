namespace Logistics.Domain.Services;

/// <summary>
/// Port (in the Ports &amp; Adapters sense) describing what the routing service needs from the
/// underlying graph — expressed in domain terms. The domain owns this interface; an adapter
/// in an outer layer satisfies it from the database. The domain never references persistence.
/// </summary>
public interface IRouteGraph
{
    Task<GraphPath?> FindShortestPathAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        CancellationToken ct = default);
}

/// <summary>A path through the warehouse graph, in domain terms.</summary>
public sealed record GraphPath(
    IReadOnlyList<string> WarehouseIds,
    double TotalDistanceKm,
    int Hops);
