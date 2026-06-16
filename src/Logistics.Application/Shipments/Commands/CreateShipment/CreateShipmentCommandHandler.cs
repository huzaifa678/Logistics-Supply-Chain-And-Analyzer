using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Entities;
using MediatR;

namespace Logistics.Application.Shipments.Commands.CreateShipment;

public sealed class CreateShipmentCommandHandler(IShipmentRepository shipments)
    : IRequestHandler<CreateShipmentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateShipmentCommand request, CancellationToken ct)
    {
        if (!await shipments.WarehouseExistsAsync(request.OriginWarehouseId, ct))
            return Result<string>.Failure($"Origin warehouse '{request.OriginWarehouseId}' does not exist.");
        if (!await shipments.WarehouseExistsAsync(request.DestinationWarehouseId, ct))
            return Result<string>.Failure($"Destination warehouse '{request.DestinationWarehouseId}' does not exist.");

        var shipment = Shipment.Create(
            request.TrackingNumber,
            request.OriginWarehouseId,
            request.DestinationWarehouseId,
            request.WeightKg,
            request.Mode);

        var id = await shipments.AddAsync(shipment, ct);
        return Result<string>.Success(id);
    }
}
