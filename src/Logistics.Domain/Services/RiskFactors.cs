using Logistics.Domain.Enums;

namespace Logistics.Domain.Services;

/// <summary>The inputs a risk factor reasons about — assembled by the risk service.</summary>
public sealed record RiskContext(
    double DistanceKm,
    int Hops,
    TransportMode Mode,
    ShipmentStatus Status,
    double WeightKg,
    int OriginCongestion,
    int DestinationCongestion);

/// <summary>One factor's contribution to the total score, with a human-readable reason.</summary>
public readonly record struct RiskContribution(double Points, string Reason);

/// <summary>
/// Strategy: a single, independent risk rule. The service composes all registered factors,
/// so a new rule is a new class wired in DI — no change to the service (Open/Closed).
/// </summary>
public interface IRiskFactor
{
    string Name { get; }
    RiskContribution Evaluate(RiskContext context);
}

/// <summary>Longer journeys accrue more exposure. ~1 point per 50 km, capped at 30.</summary>
public sealed class DistanceRiskFactor : IRiskFactor
{
    public string Name => "Distance";
    public RiskContribution Evaluate(RiskContext c)
    {
        var points = Math.Min(30, c.DistanceKm / 50.0);
        return new RiskContribution(points, $"{c.DistanceKm:F0} km in transit");
    }
}

/// <summary>Every extra handoff between warehouses adds risk. 8 points per hop beyond the first, capped at 24.</summary>
public sealed class HopCountRiskFactor : IRiskFactor
{
    public string Name => "Handoffs";
    public RiskContribution Evaluate(RiskContext c)
    {
        var extraHops = Math.Max(0, c.Hops - 1);
        var points = Math.Min(24, extraHops * 8.0);
        return new RiskContribution(points, $"{c.Hops} leg(s), {extraHops} handoff(s)");
    }
}

/// <summary>Each transport mode carries a different baseline risk (weather, theft, handling).</summary>
public sealed class TransportModeRiskFactor : IRiskFactor
{
    private static readonly IReadOnlyDictionary<TransportMode, double> ModeRisk = new Dictionary<TransportMode, double>
    {
        [TransportMode.Road] = 12,
        [TransportMode.Rail] = 8,
        [TransportMode.Sea] = 20,
        [TransportMode.Air] = 10,
        [TransportMode.Intermodal] = 15
    };

    public string Name => "Mode";
    public RiskContribution Evaluate(RiskContext c)
    {
        var points = ModeRisk.GetValueOrDefault(c.Mode, 12);
        return new RiskContribution(points, $"{c.Mode} baseline");
    }
}

/// <summary>A shipment already flagged as delayed is materially more likely to miss its SLA.</summary>
public sealed class DelayRiskFactor : IRiskFactor
{
    public string Name => "Status";
    public RiskContribution Evaluate(RiskContext c) => c.Status switch
    {
        ShipmentStatus.Delayed => new RiskContribution(25, "Already delayed"),
        ShipmentStatus.InTransit => new RiskContribution(5, "In transit"),
        _ => new RiskContribution(0, $"Status {c.Status}")
    };
}

/// <summary>Congested endpoints raise the chance of dwell time. Averages the two endpoints, capped at 20.</summary>
public sealed class CongestionRiskFactor : IRiskFactor
{
    public string Name => "Congestion";
    public RiskContribution Evaluate(RiskContext c)
    {
        var avg = (c.OriginCongestion + c.DestinationCongestion) / 2.0;
        var points = Math.Min(20, avg * 0.5);
        return new RiskContribution(points, $"{c.OriginCongestion} in / {c.DestinationCongestion} out active");
    }
}
