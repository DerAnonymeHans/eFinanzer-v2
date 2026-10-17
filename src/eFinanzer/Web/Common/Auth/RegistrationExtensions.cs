using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace eFinanzer.Common.Auth;

public static class RegistrationExtensions
{
    /// <summary>
    /// Validates Keycloak access tokens and requires one on every endpoint that does not opt out via
    /// <c>AllowAnonymous()</c>. Group checks happen in the endpoint handlers using <see cref="UserClaims"/>.
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authority = configuration.GetValue<string>("Authentication:Authority")
                        ?? throw new InvalidOperationException("'Authentication:Authority' is not configured.");
        var audience = configuration.GetValue<string>("Authentication:Audience")
                       ?? throw new InvalidOperationException("'Authentication:Audience' is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                // Keep Keycloak's claim names (sub, groups, ...) instead of mapping them to the long WS-* claim types
                options.MapInboundClaims = false;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddExceptionHandler<AccessForbiddenExceptionHandler>();

        return services;
    }
}
