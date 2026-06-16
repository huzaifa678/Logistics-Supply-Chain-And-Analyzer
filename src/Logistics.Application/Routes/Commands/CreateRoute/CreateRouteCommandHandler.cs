using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Entities;
using MediatR;

namespace Logistics.Application.Routes.Commands.CreateRoute;

public sealed class CreateRouteCommandHandler(IRouteRepository routes)
    : IRequestHandler<CreateRouteCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateRouteCommand request, CancellationToken ct)
    {
        if (!await routes.WarehouseExistsAsync(request.OriginWarehouseId, ct))
            return Result<string>.Failure($"Origin warehouse '{request.OriginWarehouseId}' does not exist.");
        if (!await routes.WarehouseExistsAsync(request.DestinationWarehouseId, ct))
            return Result<string>.Failure($"Destination warehouse '{request.DestinationWarehouseId}' does not exist.");

        var route = Route.Create(
            request.OriginWarehouseId,
            request.DestinationWarehouseId,
            request.DistanceKm,
            request.Cost,
            request.Mode);

        var id = await routes.AddAsync(route, ct);
        return Result<string>.Success(id);
    }
}
