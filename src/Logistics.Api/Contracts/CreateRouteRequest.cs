using Logistics.Domain.Enums;

namespace Logistics.Api.Contracts;

/// <summary>API request body — the public wire contract, decoupled from the command.</summary>
public sealed record CreateRouteRequest(
    string OriginWarehouseId,
    string DestinationWarehouseId,
    double DistanceKm,
    decimal Cost,
    TransportMode Mode);
