using System.Globalization;
using System.Security.Claims;
using Logistics.Application.Common.Interfaces;

namespace Logistics.Api.Middleware;

/// <summary>
/// Enforces a per-client distributed rate limit before the request reaches a controller.
/// Partitions by authenticated user id when present, otherwise by client IP — so one noisy
/// client can't exhaust the quota of others.
/// </summary>
public sealed class RateLimitingMiddleware(RequestDelegate next, IRateLimiter limiter)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var partitionKey = ResolvePartitionKey(context);
        var decision = await limiter.AcquireAsync(partitionKey, context.RequestAborted);

        if (!decision.Allowed)
        {
            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(decision.RetryAfter.TotalSeconds));
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Retry in {retryAfterSeconds}s."
            });
            return;
        }

        context.Response.Headers["X-RateLimit-Remaining"] =
            ((int)Math.Floor(decision.TokensRemaining)).ToString(CultureInfo.InvariantCulture);

        await next(context);
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseDistributedRateLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<RateLimitingMiddleware>();
}
