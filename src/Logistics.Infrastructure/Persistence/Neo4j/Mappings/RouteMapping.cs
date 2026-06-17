using Logistics.Domain.Entities;
using Logistics.Domain.Enums;
using Neo4j.Driver;

namespace Logistics.Infrastructure.Persistence.Neo4j.Mappings;

internal sealed record RouteDao(
    string Id,
    string OriginId,
    string DestinationId,
    double DistanceKm,
    double Cost,
    string Mode)
{
    public static RouteDao FromRecord(IRecord r) => new(
        r["id"].As<string>(),
        r["originId"].As<string>(),
        r["destinationId"].As<string>(),
        r["distanceKm"].As<double>(),
        r["cost"].As<double>(),
        r["mode"].As<string>());
}

internal static class RouteMapper
{
    public static Route ToDomain(RouteDao dao) => Route.Rehydrate(
        dao.Id,
        dao.OriginId,
        dao.DestinationId,
        dao.DistanceKm,
        Convert.ToDecimal(dao.Cost),
        Enum.Parse<TransportMode>(dao.Mode));

    public static object ToCreateParameters(Route r) => new
    {
        id = r.Id,
        originId = r.OriginId,
        destinationId = r.DestinationId,
        distanceKm = r.DistanceKm,
        cost = r.Cost,
        mode = r.Mode.ToString()
    };
}
