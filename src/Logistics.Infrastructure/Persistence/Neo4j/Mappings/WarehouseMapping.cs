using Logistics.Domain.Entities;
using Logistics.Domain.ValueObjects;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Mappings;

/// <summary>Flat projection of a (:Warehouse) node. Tolerant of nodes seeded without all props.</summary>
internal sealed record WarehouseDao(
    string Id,
    string Name,
    double Latitude,
    double Longitude,
    int CapacityUnits)
{
    public static WarehouseDao FromRecord(IRecord r) => new(
        r["id"].As<string>(),
        r["name"].As<string>() ?? string.Empty,
        r["latitude"].As<double?>() ?? 0,
        r["longitude"].As<double?>() ?? 0,
        r["capacityUnits"].As<int?>() ?? 0);
}

internal static class WarehouseMapper
{
    public static Warehouse ToDomain(WarehouseDao dao) => Warehouse.Rehydrate(
        dao.Id,
        dao.Name,
        new GeoLocation(dao.Latitude, dao.Longitude),
        dao.CapacityUnits);

    public static object ToCreateParameters(Warehouse w) => new
    {
        id = w.Id,
        name = w.Name,
        latitude = w.Location.Latitude,
        longitude = w.Location.Longitude,
        capacityUnits = w.CapacityUnits,
    };
}
