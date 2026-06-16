using Logistics.Domain.Entities;

namespace Logistics.Domain.Services;

/// <summary>
/// Domain service: scores the delivery risk of a shipment by composing independent risk factors
/// over its route. Pure — it depends on the <see cref="IRouteGraph"/> and
/// <see cref="IWarehouseCongestionProvider"/> ports and the <see cref="IRiskFactor"/> strategies,
/// never on persistence or HTTP.
/// </summary>
public interface IShipmentRiskService
{
    Task<RiskAssessment?> AssessAsync(Shipment shipment, CancellationToken ct = default);
}

public enum RiskBand { Low, Medium, High }

public sealed record RiskFactorResult(string Name, double Points, string Reason);

public sealed record RiskAssessment(
    string ShipmentId,
    double Score,
    RiskBand Band,
    IReadOnlyList<RiskFactorResult> Factors);

public sealed class ShipmentRiskService(
    IRouteGraph graph,
    IWarehouseCongestionProvider congestion,
    IEnumerable<IRiskFactor> factors) : IShipmentRiskService
{
    // Materialize once so the collection is stable and enumerable repeatedly.
    private readonly IReadOnlyList<IRiskFactor> _factors = factors.ToList();

    public async Task<RiskAssessment?> AssessAsync(Shipment shipment, CancellationToken ct = default)
    {
        var path = await graph.FindShortestPathAsync(
            shipment.OriginWarehouseId, shipment.DestinationWarehouseId, ct);
        if (path is null)
            return null; // no route → can't assess

        var originCongestion = await congestion.GetActiveShipmentCountAsync(shipment.OriginWarehouseId, ct);
        var destCongestion = await congestion.GetActiveShipmentCountAsync(shipment.DestinationWarehouseId, ct);

        var context = new RiskContext(
            path.TotalDistanceKm, path.Hops, shipment.Mode, shipment.Status,
            shipment.WeightKg, originCongestion, destCongestion);

        var results = _factors
            .Select(f =>
            {
                var c = f.Evaluate(context);
                return new RiskFactorResult(f.Name, Math.Round(c.Points, 1), c.Reason);
            })
            .ToList();

        var score = Math.Clamp(results.Sum(r => r.Points), 0, 100);
        var band = score switch
        {
            >= 66 => RiskBand.High,
            >= 33 => RiskBand.Medium,
            _ => RiskBand.Low
        };

        return new RiskAssessment(shipment.Id, Math.Round(score, 1), band, results);
    }
}
