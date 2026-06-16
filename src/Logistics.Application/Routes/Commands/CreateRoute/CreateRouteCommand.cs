using Logistics.Application.Common.Models;
using Logistics.Domain.Enums;
using MediatR;

namespace Logistics.Application.Routes.Commands.CreateRoute;

/// <summary>Write operation: create a weighted connection between two warehouses.</summary>
public sealed record CreateRouteCommand(
    string OriginWarehouseId,
    string DestinationWarehouseId,
    double DistanceKm,
    decimal Cost,
    TransportMode Mode) : IRequest<Result<string>>;
