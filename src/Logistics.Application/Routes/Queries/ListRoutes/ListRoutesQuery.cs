using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Routes.Queries.ListRoutes;

/// <summary>Lists all routes, enriched with warehouse names for display.</summary>
public sealed record ListRoutesQuery : IRequest<Result<IReadOnlyList<RouteSummaryDto>>>;

public sealed record RouteSummaryDto(
    string Id,
    string OriginWarehouseId,
    string OriginName,
    string DestinationWarehouseId,
    string DestinationName,
    double DistanceKm,
    decimal Cost,
    string Mode);

public sealed class ListRoutesQueryHandler(
    IRouteRepository routes,
    IWarehouseRepository warehouses)
    : IRequestHandler<ListRoutesQuery, Result<IReadOnlyList<RouteSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<RouteSummaryDto>>> Handle(ListRoutesQuery request, CancellationToken ct)
    {
        var all = await routes.ListAsync(ct);
        var nameById = (await warehouses.ListAsync(ct))
            .ToDictionary(w => w.Id, w => w.Name);

        string Name(string id) =>
            nameById.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n) ? n : id;

        var dtos = all
            .Select(r => new RouteSummaryDto(
                r.Id,
                r.OriginId, Name(r.OriginId),
                r.DestinationId, Name(r.DestinationId),
                r.DistanceKm, r.Cost, r.Mode.ToString()))
            .OrderBy(r => r.OriginName)
            .ThenBy(r => r.DestinationName)
            .ToList();

        return Result<IReadOnlyList<RouteSummaryDto>>.Success(dtos);
    }
}
