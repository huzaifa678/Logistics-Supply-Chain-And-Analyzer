namespace Logistics.Api.Contracts;

/// <summary>Body for POST /api/warehouses.</summary>
public sealed record CreateWarehouseRequest(
    string Name,
    double Latitude,
    double Longitude,
    int CapacityUnits);
