using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using MediatR;

namespace Logistics.Application.Routes.Queries.GetRouteById;

public sealed record GetRouteByIdQuery(string Id) : IRequest<Result<RouteDto>>;

/// <summary>Read-model DTO for a route — the application's public shape, mapped from the entity.</summary>
public sealed record RouteDto(
    string Id,
    string OriginWarehouseId,
    string DestinationWarehouseId,
    double DistanceKm,
    decimal Cost,
    TransportMode Mode)
{
    public static RouteDto FromDomain(Route r) => new(
        r.Id, r.OriginId, r.DestinationId, r.DistanceKm, r.Cost, r.Mode);
}

public sealed class GetRouteByIdQueryHandler(IRouteRepository routes)
    : IRequestHandler<GetRouteByIdQuery, Result<RouteDto>>
{
    public async Task<Result<RouteDto>> Handle(GetRouteByIdQuery request, CancellationToken ct)
    {
        var route = await routes.GetByIdAsync(request.Id, ct);
        return route is null
            ? Result<RouteDto>.Failure($"Route '{request.Id}' was not found.")
            : Result<RouteDto>.Success(RouteDto.FromDomain(route));
    }
}
