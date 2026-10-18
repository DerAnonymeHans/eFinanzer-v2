using System.Net;
using System.Net.Http.Json;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.GetUsers.Tasks.GetUser.Models;
using eFinanzer.Tests.Infrastructure;
using NSubstitute;

namespace eFinanzer.Tests.Features.GetUsers.Endpoints;

/// <summary>
/// The task is covered by its own tests, these only cover what the endpoint adds: authorization and the mapping to
/// HTTP
/// </summary>
public class GetUsersEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/get-users";

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task GetUser_AnonymousUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-user?username=alice",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_ExistingUser_ReturnsUser()
    {
        // Arrange
        _factory.KeycloakUserClient.GetUserByUsername("alice", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Ok(new KeycloakUser
            {
                Id = "alice-id", Username = "alice", FirstName = "Alice", LastName = "Liddell",
            }));
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-user?username=alice",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<GetUserDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        Assert.Equal("alice", user.Username);
        Assert.Equal("Alice", user.FirstName);
        Assert.Equal("Liddell", user.LastName);
    }

    [Fact]
    public async Task GetUser_UnknownUser_ReturnsNotFound()
    {
        // Arrange
        _factory.KeycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-user?username=mallory",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
