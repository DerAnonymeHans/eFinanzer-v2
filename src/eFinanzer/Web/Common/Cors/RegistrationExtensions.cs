namespace eFinanzer.Common.Cors;

public static class RegistrationExtensions
{
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Lets frontends from other origins call the API, e.g. the React dev server against the deployed backend. The
    /// deployed frontend is served from the same origin and does not need it.
    /// Only the bearer token in the Authorization header authenticates, so no credentials (cookies) are allowed.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [];

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
