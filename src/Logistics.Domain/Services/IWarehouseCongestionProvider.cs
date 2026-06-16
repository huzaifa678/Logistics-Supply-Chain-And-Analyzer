namespace Logistics.Domain.Services;

/// <summary>
/// Port: how congested a warehouse is right now (active shipments touching it). The risk
/// service declares this need in domain terms; an adapter satisfies it from the database.
/// </summary>
public interface IWarehouseCongestionProvider
{
    Task<int> GetActiveShipmentCountAsync(string warehouseId, CancellationToken ct = default);
}
