using Logistics.Domain.Enums;
using Logistics.Domain.Exceptions;

namespace Logistics.Domain.Services;

/// <summary>
/// Strategy: the speed/cost characteristics of one transport mode. Estimation logic stays
/// open for extension — a new mode is a new class, with no change to the routing service.
/// </summary>
public interface ITransportModeProfile
{
    TransportMode Mode { get; }
    double AverageSpeedKmph { get; }
    decimal CostPerKm { get; }
}

public sealed class RoadProfile : ITransportModeProfile
{
    public TransportMode Mode => TransportMode.Road;
    public double AverageSpeedKmph => 70;
    public decimal CostPerKm => 1.20m;
}

public sealed class RailProfile : ITransportModeProfile
{
    public TransportMode Mode => TransportMode.Rail;
    public double AverageSpeedKmph => 90;
    public decimal CostPerKm => 0.60m;
}

public sealed class SeaProfile : ITransportModeProfile
{
    public TransportMode Mode => TransportMode.Sea;
    public double AverageSpeedKmph => 35;
    public decimal CostPerKm => 0.25m;
}

public sealed class AirProfile : ITransportModeProfile
{
    public TransportMode Mode => TransportMode.Air;
    public double AverageSpeedKmph => 800;
    public decimal CostPerKm => 4.50m;
}

public sealed class IntermodalProfile : ITransportModeProfile
{
    public TransportMode Mode => TransportMode.Intermodal;
    public double AverageSpeedKmph => 60;
    public decimal CostPerKm => 0.80m;
}

/// <summary>Selects the right strategy for a mode (Strategy + simple Factory).</summary>
public interface ITransportModeProfileResolver
{
    ITransportModeProfile Resolve(TransportMode mode);
}

public sealed class TransportModeProfileResolver : ITransportModeProfileResolver
{
    private readonly IReadOnlyDictionary<TransportMode, ITransportModeProfile> _profiles;

    public TransportModeProfileResolver(IEnumerable<ITransportModeProfile> profiles)
        => _profiles = profiles.ToDictionary(p => p.Mode);

    public ITransportModeProfile Resolve(TransportMode mode)
        => _profiles.TryGetValue(mode, out var profile)
            ? profile
            : throw new DomainException($"No transport profile registered for mode '{mode}'.");
}
