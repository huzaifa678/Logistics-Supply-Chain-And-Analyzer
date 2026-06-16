using Logistics.Application.Common.Interfaces;
using Logistics.Application.Shipments.Queries.GetShipmentByTracking;
using Logistics.Domain.Enums;
using MediatR;

namespace Logistics.Application.Shipments.Queries.StreamShipmentsByStatus;

/// <summary>
/// Streams shipments with a given status. The response itself is an <see cref="IAsyncEnumerable{T}"/>,
/// so nothing is buffered: each DTO is yielded as its record arrives from the database.
/// </summary>
public sealed record StreamShipmentsByStatusQuery(ShipmentStatus Status)
    : IRequest<IAsyncEnumerable<ShipmentDto>>;

public sealed class StreamShipmentsByStatusQueryHandler(IShipmentRepository shipments)
    : IRequestHandler<StreamShipmentsByStatusQuery, IAsyncEnumerable<ShipmentDto>>
{
    public Task<IAsyncEnumerable<ShipmentDto>> Handle(StreamShipmentsByStatusQuery request, CancellationToken ct)
        => Task.FromResult(Project(request.Status, ct));

    private async IAsyncEnumerable<ShipmentDto> Project(
        ShipmentStatus status,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var s in shipments.StreamByStatusAsync(status, ct))
        {
            yield return new ShipmentDto(
                s.Id, s.TrackingNumber, s.OriginWarehouseId, s.DestinationWarehouseId,
                s.WeightKg, s.Mode, s.Status, s.CreatedAt, s.EstimatedArrival, s.DeliveredAt);
        }
    }
}
