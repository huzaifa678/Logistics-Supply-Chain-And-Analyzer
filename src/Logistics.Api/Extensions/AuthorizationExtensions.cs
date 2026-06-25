using Logistics.Domain.Identity;

namespace Logistics.Api.Extensions;

/// <summary>
/// Authorization policy names. Reference these from <c>[Authorize(Policy = ...)]</c> instead of
/// scattering magic role strings (e.g. "Operator,Admin") across controllers. The policies
/// themselves are defined in <see cref="AuthorizationExtensions.AddAppAuthorization"/>.
/// </summary>
public static class Policies
{
    /// <summary>Operator or Admin — write operations on domain data (shipments, routes, warehouses).</summary>
    public const string RequireOperator = nameof(RequireOperator);

    /// <summary>Admin only — user and system administration.</summary>
    public const string RequireAdmin = nameof(RequireAdmin);
}

public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers the app's role-based authorization policies. Role names are derived from the
    /// domain <see cref="Role"/> enum so they can't drift from the values stamped into JWTs.
    /// </summary>
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.RequireOperator, policy =>
                policy.RequireRole(nameof(Role.Operator), nameof(Role.Admin)))
            .AddPolicy(Policies.RequireAdmin, policy =>
                policy.RequireRole(nameof(Role.Admin)));

        return services;
    }
}
