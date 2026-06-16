using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Routes.Queries.GetShortestPath;

public sealed class GetShortestPathQueryHandler(IGraphAnalyticsRepository analytics)
    : IRequestHandler<GetShortestPathQuery, Result<ShortestPathDto>>
{
    public async Task<Result<ShortestPathDto>> Handle(GetShortestPathQuery request, CancellationToken ct)
    {
        var path = await analytics.GetShortestPathAsync(
            request.OriginWarehouseId, request.DestinationWarehouseId, ct);

        if (path is null)
            return Result<ShortestPathDto>.Failure("No path exists between the given warehouses.");

        return Result<ShortestPathDto>.Success(
            new ShortestPathDto(path.WarehouseIds, path.TotalDistanceKm, path.Hops));
    }
}
