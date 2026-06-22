namespace Logistics.Api.Extensions;

public static class CorsExtensions
{
    public const string FrontendPolicy = "frontend";

    /// <summary>
    /// Allows the SPA origins to call the API. Origins come from config ("Cors:AllowedOrigins")
    /// so each environment lists its own; defaults cover the Angular dev server and SSR server.
    /// Only needed when the SPA calls the API cross-origin — behind a same-origin proxy/ingress
    /// (the default setup) CORS is a no-op.
    /// </summary>
    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200", "http://localhost:4000"];

        services.AddCors(options =>
            options.AddPolicy(FrontendPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }
}
