using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Routes.Queries.GetShortestPath;

/// <summary>Read operation: weighted shortest path between two warehouses.</summary>
public sealed record GetShortestPathQuery(string OriginWarehouseId, string DestinationWarehouseId)
    : IRequest<Result<ShortestPathDto>>;

public sealed record ShortestPathDto(
    IReadOnlyList<string> WarehouseIds,
    double TotalDistanceKm,
    int Hops);
