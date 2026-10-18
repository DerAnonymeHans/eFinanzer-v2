using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.GetUsers.Tasks.GetUser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.GetUsers.Tasks;

public class GetUserTaskTests
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private GetUserTask MyTask => new(_keycloakUserClient, _cache, NullLogger<GetUserTask>.Instance);

    [Fact]
    public async Task Get_ExistingUser_ReturnsUser()
    {
        // Arrange
        _keycloakUserClient.GetUserByUsername("alice", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Ok(new KeycloakUser
            {
                Id = "alice-id", Username = "alice", FirstName = "Alice", LastName = "Liddell",
            }));

        // Act
        var result = await MyTask.Get("alice", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("alice", result.Value.Username);
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal("Liddell", result.Value.LastName);
    }

    [Fact]
    public async Task Get_CalledTwice_LoadsUserFromKeycloakOnlyOnce()
    {
        // Arrange
        _keycloakUserClient.GetUserByUsername("alice", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Ok(new KeycloakUser { Id = "alice-id", Username = "alice" }));

        // Act
        await MyTask.Get("alice", CancellationToken.None);
        var result = await MyTask.Get("alice", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("alice", result.Value.Username);
        await _keycloakUserClient.Received(1).GetUserByUsername("alice", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_UnknownUser_ReturnsErrorAndDoesNotCacheIt()
    {
        // Arrange
        _keycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));

        // Act
        var result = await MyTask.Get("mallory", CancellationToken.None);
        await MyTask.Get("mallory", CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<NotFoundError>(result.Error);
        await _keycloakUserClient.Received(2).GetUserByUsername("mallory", Arg.Any<CancellationToken>());
    }
}
