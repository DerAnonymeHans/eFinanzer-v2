using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using Microsoft.Extensions.Options;

namespace eFinanzer.Common.Integrations.Keycloak;

public static class RegistrationExtensions
{
    public static IServiceCollection AddKeycloakAdmin(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KeycloakAdminOptions>()
            .Bind(configuration.GetSection(KeycloakAdminOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpClient(KeycloakAdminTokenProvider.HttpClientName);
        services.AddSingleton<KeycloakAdminTokenProvider>();
        services.AddTransient<KeycloakAdminTokenHandler>();

        services.AddHttpClient<IKeycloakUserClient, KeycloakUserClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<KeycloakAdminOptions>>().Value;
                httpClient.BaseAddress = options.AdminApiBaseAddress;
            })
            .AddHttpMessageHandler<KeycloakAdminTokenHandler>();

        return services;
    }
}
