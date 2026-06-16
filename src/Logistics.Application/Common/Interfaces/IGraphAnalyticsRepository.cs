namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Read-side graph queries (shortest path, centrality, bottleneck detection).
/// Returns plain DTOs so the analytics layer stays free of driver types.
/// </summary>
public interface IGraphAnalyticsRepository
{
    Task<ShortestPathResult?> GetShortestPathAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        CancellationToken ct = default);

    /// <summary>Count of active (non-terminal) shipments originating at or destined for a warehouse.</summary>
    Task<int> GetActiveShipmentCountAsync(string warehouseId, CancellationToken ct = default);
}

/// <summary>Projection returned by a weighted shortest-path traversal.</summary>
public sealed record ShortestPathResult(
    IReadOnlyList<string> WarehouseIds,
    double TotalDistanceKm,
    int Hops);
