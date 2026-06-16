using Logistics.Application.Common.Models;
using Logistics.Domain.Enums;
using Logistics.Domain.Services;
using MediatR;

namespace Logistics.Application.Routes.Queries.EstimateRoute;

/// <summary>Estimate duration + cost for routing a shipment between two warehouses by a mode.</summary>
public sealed record EstimateRouteQuery(string Origin, string Destination, TransportMode Mode)
    : IRequest<Result<RouteEstimateDto>>;

public sealed record RouteEstimateDto(
    IReadOnlyList<string> WarehouseIds,
    double TotalDistanceKm,
    int Hops,
    double EstimatedHours,
    decimal EstimatedCost,
    TransportMode Mode)
{
    public static RouteEstimateDto FromDomain(RouteEstimate e) => new(
        e.WarehouseIds, e.TotalDistanceKm, e.Hops,
        Math.Round(e.EstimatedDuration.TotalHours, 2), e.EstimatedCost, e.Mode);
}

/// <summary>
/// Thin use-case handler — delegates the business logic to the domain service and shapes the
/// result. The handler orchestrates; the domain service decides.
/// </summary>
public sealed class EstimateRouteQueryHandler(IShipmentRoutingService routing)
    : IRequestHandler<EstimateRouteQuery, Result<RouteEstimateDto>>
{
    public async Task<Result<RouteEstimateDto>> Handle(EstimateRouteQuery request, CancellationToken ct)
    {
        var estimate = await routing.EstimateAsync(request.Origin, request.Destination, request.Mode, ct);
        return estimate is null
            ? Result<RouteEstimateDto>.Failure("No path exists between the given warehouses.")
            : Result<RouteEstimateDto>.Success(RouteEstimateDto.FromDomain(estimate));
    }
}
