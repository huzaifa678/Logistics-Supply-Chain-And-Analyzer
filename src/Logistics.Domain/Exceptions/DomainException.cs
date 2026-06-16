namespace Logistics.Domain.Exceptions;

/// <summary>Base class for all domain rule violations.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class RouteNotFoundException : DomainException
{
    public RouteNotFoundException(string routeId)
        : base($"Route '{routeId}' was not found.") { }
}
