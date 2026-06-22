using Logistics.Infrastructure.Persistence.Neo4j;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Logistics.Infrastructure.Health;

/// <summary>
/// Readiness check: the API is only "ready" to serve traffic if it can reach Neo4j (its system
/// of record). Used by the Kubernetes readiness probe so a pod that loses its database is pulled
/// out of the Service's endpoints instead of failing requests.
/// </summary>
public sealed class Neo4jHealthCheck(Neo4jContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context_, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.VerifyConnectivityAsync();
            return HealthCheckResult.Healthy("Neo4j reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Neo4j unreachable.", ex);
        }
    }
}
