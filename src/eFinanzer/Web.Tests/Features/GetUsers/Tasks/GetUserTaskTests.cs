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

    [Fact]
    public async Task Search_MatchingUsers_ReturnsThemSortedByName()
    {
        // Arrange
        _keycloakUserClient.SearchUser("li", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser[]>.Ok([
                new KeycloakUser { Id = "bob-id", Username = "bob", FirstName = "Bob", LastName = "Liddell" },
                new KeycloakUser { Id = "alice-id", Username = "alice", FirstName = "Alice", LastName = "Liddell" },
                new KeycloakUser { Id = "carl-id", Username = "carl", FirstName = "Carl", LastName = "Lindgren" },
                new KeycloakUser { Id = "abe-id", Username = "abe", FirstName = "Abe", LastName = "Lewis" },
            ]));

        // Act
        var result = await MyTask.Search("li", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["abe", "alice", "bob", "carl"], result.Value.Select(x => x.Username));
        Assert.Equal("Alice", result.Value[1].FirstName);
        Assert.Equal("Liddell", result.Value[1].LastName);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        // Arrange
        _keycloakUserClient.SearchUser("zz", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser[]>.Ok([]));

        // Act
        var result = await MyTask.Search("zz", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Search_SurroundingWhitespace_IsTrimmed()
    {
        // Arrange
        _keycloakUserClient.SearchUser(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser[]>.Ok([]));

        // Act
        await MyTask.Search("  alice liddell ", CancellationToken.None);

        // Assert
        await _keycloakUserClient.Received(1).SearchUser("alice liddell", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" a ")]
    public async Task Search_TooShort_ReturnsInvalidInputWithoutQueryingKeycloak(string? search)
    {
        // Act
        var result = await MyTask.Search(search, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<InvalidInputError>(result.Error);
        await _keycloakUserClient.DidNotReceiveWithAnyArgs().SearchUser(default!, default);
    }

    [Fact]
    public async Task Search_TooLong_ReturnsInvalidInput()
    {
        // Act
        var result = await MyTask.Search(new string('a', GetUserTask.SearchStringMaxLength + 1),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.IsType<InvalidInputError>(result.Error);
    }
}
