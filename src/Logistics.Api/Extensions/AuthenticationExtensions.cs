using System.Text;
using Logistics.Application.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Logistics.Api.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>Configures JWT bearer validation from the "Auth" config section.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>()
                   ?? new AuthSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = auth.Issuer,
                    ValidAudience = auth.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Authorization policies are registered separately — see AddAppAuthorization().
        return services;
    }
}
