using Logistics.Application.Common.Interfaces;
using Logistics.Application.Common.Models;
using Logistics.Domain.Enums;
using MediatR;

namespace Logistics.Application.Shipments.Queries.GetShipmentByTracking;

public sealed record GetShipmentByTrackingQuery(string TrackingNumber)
    : IRequest<Result<ShipmentDto>>;

public sealed record ShipmentDto(
    string Id,
    string TrackingNumber,
    string OriginWarehouseId,
    string DestinationWarehouseId,
    double WeightKg,
    TransportMode Mode,
    ShipmentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EstimatedArrival,
    DateTimeOffset? DeliveredAt);

public sealed class GetShipmentByTrackingQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<GetShipmentByTrackingQuery, Result<ShipmentDto>>
{
    public async Task<Result<ShipmentDto>> Handle(GetShipmentByTrackingQuery request, CancellationToken ct)
    {
        var s = await shipments.GetByTrackingNumberAsync(request.TrackingNumber, ct);
        if (s is null)
            return Result<ShipmentDto>.Failure($"No shipment found for tracking number '{request.TrackingNumber}'.");

        return Result<ShipmentDto>.Success(new ShipmentDto(
            s.Id, s.TrackingNumber, s.OriginWarehouseId, s.DestinationWarehouseId,
            s.WeightKg, s.Mode, s.Status, s.CreatedAt, s.EstimatedArrival, s.DeliveredAt));
    }
}
