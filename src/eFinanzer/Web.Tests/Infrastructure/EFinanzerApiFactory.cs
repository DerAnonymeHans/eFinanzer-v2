using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Runs the app against the test database, with <see cref="TestAuthHandler"/> instead of Keycloak tokens and a
/// substituted Keycloak admin client
/// </summary>
public class EFinanzerApiFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    public IKeycloakUserClient KeycloakUserClient { get; } = Substitute.For<IKeycloakUserClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", database.ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<IKeycloakUserClient>();
            services.AddSingleton(KeycloakUserClient);
        });
    }

    /// <summary>
    /// Client authenticated as a user in the given groups
    /// </summary>
    public HttpClient CreateAuthorizedClient(params string[] groupPaths)
    {
        if (groupPaths.Length == 0)
        {
            // Any group is enough for a default authorized client
            groupPaths = [KeycloakGroups.AccountingUnitsGroup];
        }
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.GroupsHeader, string.Join(',', groupPaths));
        return client;
    }

}
