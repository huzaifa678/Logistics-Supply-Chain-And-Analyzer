using Logistics.Application.Common.Interfaces;
using Logistics.Domain.Services;

namespace Logistics.Application.Common.Adapters;

/// <summary>
/// Adapter: satisfies the domain's <see cref="IWarehouseCongestionProvider"/> port using the
/// persistence-facing <see cref="IGraphAnalyticsRepository"/>. Same DB↔service seam as
/// <see cref="GraphAnalyticsRouteGraphAdapter"/> — the risk service never touches the repository.
/// </summary>
public sealed class WarehouseCongestionAdapter(IGraphAnalyticsRepository analytics)
    : IWarehouseCongestionProvider
{
    public Task<int> GetActiveShipmentCountAsync(string warehouseId, CancellationToken ct = default)
        => analytics.GetActiveShipmentCountAsync(warehouseId, ct);
}
