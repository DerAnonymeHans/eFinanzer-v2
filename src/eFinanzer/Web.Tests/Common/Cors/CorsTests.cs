using System.Net;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Common.Cors;

/// <summary>
/// The local frontend (see Cors:AllowedOrigins in appsettings.json) calls the API from another origin
/// </summary>
public class CorsTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string Url = "/api/get-user-access/my-user-access";
    private const string AllowedOrigin = "http://localhost:3000";

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    private static HttpRequestMessage Preflight(string origin) => new(HttpMethod.Options, Url)
    {
        Headers =
        {
            { "Origin", origin },
            { "Access-Control-Request-Method", "GET" },
            { "Access-Control-Request-Headers", "authorization" },
        },
    };

    [Fact]
    public async Task Preflight_AllowedOrigin_IsAnsweredWithoutToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(Preflight(AllowedOrigin), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("authorization", response.Headers.GetValues("Access-Control-Allow-Headers").Single(),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Preflight_UnknownOrigin_IsNotAllowed()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(Preflight("https://example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Request_AllowedOrigin_ExposesUnauthorizedResponseToTheFrontend()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, Url) { Headers = { { "Origin", AllowedOrigin } } };

        // Act
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }
}
