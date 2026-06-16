using Logistics.Domain.Enums;

namespace Logistics.Domain.Services;

/// <summary>
/// Domain service: cross-aggregate business logic that belongs to no single entity. It combines
/// a path through the warehouse graph with mode-specific speed/cost rules to estimate a route.
/// Pure — depends only on the <see cref="IRouteGraph"/> port and the mode strategies, never on
/// MediatR, the database, or HTTP.
/// </summary>
public interface IShipmentRoutingService
{
    Task<RouteEstimate?> EstimateAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        TransportMode mode,
        CancellationToken ct = default);
}

/// <summary>Result of a routing estimate.</summary>
public sealed record RouteEstimate(
    IReadOnlyList<string> WarehouseIds,
    double TotalDistanceKm,
    int Hops,
    TimeSpan EstimatedDuration,
    decimal EstimatedCost,
    TransportMode Mode);

public sealed class ShipmentRoutingService(
    IRouteGraph graph,
    ITransportModeProfileResolver profiles) : IShipmentRoutingService
{
    public async Task<RouteEstimate?> EstimateAsync(
        string originWarehouseId,
        string destinationWarehouseId,
        TransportMode mode,
        CancellationToken ct = default)
    {
        var path = await graph.FindShortestPathAsync(originWarehouseId, destinationWarehouseId, ct);
        if (path is null)
            return null;

        var profile = profiles.Resolve(mode);

        var duration = TimeSpan.FromHours(path.TotalDistanceKm / profile.AverageSpeedKmph);
        var cost = (decimal)path.TotalDistanceKm * profile.CostPerKm;

        return new RouteEstimate(
            path.WarehouseIds, path.TotalDistanceKm, path.Hops, duration, cost, mode);
    }
}
