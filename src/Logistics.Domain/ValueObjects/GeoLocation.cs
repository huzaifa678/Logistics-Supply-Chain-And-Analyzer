namespace Logistics.Domain.ValueObjects;

/// <summary>
/// Immutable latitude/longitude pair. Value objects compare by value, not identity.
/// </summary>
public readonly record struct GeoLocation
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoLocation(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString() => $"({Latitude:F4}, {Longitude:F4})";
}
