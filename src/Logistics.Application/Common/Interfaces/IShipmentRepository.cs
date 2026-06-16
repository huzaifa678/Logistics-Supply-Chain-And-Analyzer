using Logistics.Domain.Entities;
using Logistics.Domain.Enums;

namespace Logistics.Application.Common.Interfaces;

public interface IShipmentRepository
{
    Task<string> AddAsync(Shipment shipment, CancellationToken ct = default);
    Task<Shipment?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct = default);
    Task UpdateAsync(Shipment shipment, CancellationToken ct = default);
    Task<bool> WarehouseExistsAsync(string warehouseId, CancellationToken ct = default);

    /// <summary>
    /// Streams shipments lazily — records are pulled from the Neo4j cursor one at a time
    /// and never fully buffered in memory. Suitable for large result sets / exports.
    /// </summary>
    IAsyncEnumerable<Shipment> StreamByStatusAsync(ShipmentStatus status, CancellationToken ct = default);
}
