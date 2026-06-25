using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using MediatR;

namespace Logistics.Application.Warehouses.Queries.ListWarehouses;

/// <summary>Lists all warehouses (for management dropdowns and the warehouses view).</summary>
public sealed record ListWarehousesQuery : IRequest<Result<IReadOnlyList<WarehouseDto>>>;

public sealed record WarehouseDto(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int CapacityUnits);

public sealed class ListWarehousesQueryHandler(IWarehouseRepository warehouses)
    : IRequestHandler<ListWarehousesQuery, Result<IReadOnlyList<WarehouseDto>>>
{
    public async Task<Result<IReadOnlyList<WarehouseDto>>> Handle(ListWarehousesQuery request, CancellationToken ct)
    {
        var all = await warehouses.ListAsync(ct);
        var dtos = all
            .Select(w => new WarehouseDto(w.Id, w.Name, w.Location.Latitude, w.Location.Longitude, w.CapacityUnits))
            .ToList();

        return Result<IReadOnlyList<WarehouseDto>>.Success(dtos);
    }
}
