using System.Net.Http.Headers;

namespace eFinanzer.Common.Integrations.Keycloak;

/// <summary>
/// Adds the service account token to every request sent to the admin REST API
/// </summary>
public class KeycloakAdminTokenHandler(KeycloakAdminTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessToken(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
