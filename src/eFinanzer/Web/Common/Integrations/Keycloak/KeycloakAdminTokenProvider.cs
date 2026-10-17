using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace eFinanzer.Common.Integrations.Keycloak;

/// <summary>
/// Gets access tokens for the admin REST API via the client credentials flow and caches them until shortly before
/// they expire. Registered as singleton, so all requests share one token.
/// </summary>
public class KeycloakAdminTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakAdminOptions> options,
    TimeProvider timeProvider)
{
    public const string HttpClientName = "KeycloakAdminToken";

    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessToken(CancellationToken ct)
    {
        if (_accessToken is not null && timeProvider.GetUtcNow() < _expiresAt)
            return _accessToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Another request may have refreshed the token while this one was waiting
            if (_accessToken is not null && timeProvider.GetUtcNow() < _expiresAt)
                return _accessToken;

            var tokenResponse = await RequestToken(ct);
            _accessToken = tokenResponse.AccessToken;
            _expiresAt = timeProvider.GetUtcNow() + TimeSpan.FromSeconds(tokenResponse.ExpiresIn) - ExpiryBuffer;

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<TokenResponse> RequestToken(CancellationToken ct)
    {
        var settings = options.Value;
        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        using var response = await httpClient.PostAsync(settings.TokenEndpoint, new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
            }), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
               ?? throw new InvalidOperationException("Keycloak returned an empty token response");
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
