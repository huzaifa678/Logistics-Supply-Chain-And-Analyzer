using Logistics.Application.Shipments.Commands.UpdateShipmentStatus;
using Logistics.Domain.Enums;

namespace Logistics.Api.Contracts;

public sealed record CreateShipmentRequest(
    string TrackingNumber,
    string OriginWarehouseId,
    string DestinationWarehouseId,
    string CustomerPhone,
    double WeightKg,
    TransportMode Mode);

public sealed record UpdateShipmentStatusRequest(
    ShipmentTransition Transition,
    DateTimeOffset? EstimatedArrival,
    string? Reason);
