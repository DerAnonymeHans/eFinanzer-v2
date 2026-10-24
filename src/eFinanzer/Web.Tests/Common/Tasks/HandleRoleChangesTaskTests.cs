using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Common.Tasks;

/// <summary>
/// Uses the administration roles as an example, the task is the same for every role enum
/// </summary>
public class HandleRoleChangesTaskTests
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private HandleRoleChangesTask MyTask => new(_keycloakUserClient, NullLogger<HandleRoleChangesTask>.Instance);

    public HandleRoleChangesTaskTests()
    {
        _keycloakUserClient.SetupGroups(Enum.GetValues<AdministrationRole>()
            .Select(KeycloakGroups.GetAdministrationRoleGroup)
            .ToArray());
    }

    private static string RoleGroupId(AdministrationRole role) => KeycloakUserClientExtensions.AdministrationRoleGroup(role).Id;

    private static UserWithRoles<AdministrationRole> User(string userName, params AdministrationRole[] roles) => new(userName, roles);

    private static RoleChange<AdministrationRole> Change(string userName, AdministrationRole role, RoleChangeType type) => new()
    {
        Username = userName,
        UserId = $"id-{userName}",
        Role = role,
        KeycloakGroupId = RoleGroupId(role),
        Type = type,
    };

    private Task<Result<RoleChange<AdministrationRole>[]>> GetRoleChanges(
        UserWithRoles<AdministrationRole>[] rolesBefore, UserWithRoles<AdministrationRole>[] rolesAfter)
        => MyTask.GetRoleChanges(rolesBefore, rolesAfter, KeycloakGroups.GetAdministrationRoleGroup, CancellationToken.None);

    [Fact]
    public async Task GetRoleChanges_UnchangedRoles_ReturnsNoChangesWithoutLoadingUsersOrGroups()
    {
        // Arrange
        var users = new[] { User("alice", AdministrationRole.FinanceAdministrator, AdministrationRole.FinanceReviewer) };

        // Act
        var result = await GetRoleChanges(users, users);

        // Assert
        Assert.Empty(result.Value);
        await _keycloakUserClient.DidNotReceive().GetUserByUsername(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().GetGroupIdFromPath(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRoleChanges_ChangedRoles_ReturnsOnlyTheDifferenceWithRemovalsFirst()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice", "bob", "carol");
        var before = new[]
        {
            User("alice", AdministrationRole.FinanceAdministrator, AdministrationRole.FinanceReviewer),
            User("bob", AdministrationRole.HeadFinanceOfficer),
        };
        var after = new[]
        {
            User("alice", AdministrationRole.FinanceReviewer, AdministrationRole.HeadFinanceOfficer),
            User("carol", AdministrationRole.FinanceReviewer),
        };

        // Act
        var result = await GetRoleChanges(before, after);

        // Assert
        var changes = result.Value;
        Assert.Equal(4, changes.Length);
        Assert.Equivalent(new[]
        {
            Change("alice", AdministrationRole.FinanceAdministrator, RoleChangeType.Removed),
            Change("bob", AdministrationRole.HeadFinanceOfficer, RoleChangeType.Removed),
        }, changes[..2]);
        Assert.Equivalent(new[]
        {
            Change("alice", AdministrationRole.HeadFinanceOfficer, RoleChangeType.Added),
            Change("carol", AdministrationRole.FinanceReviewer, RoleChangeType.Added),
        }, changes[2..]);
    }

    /// <summary>
    /// Keycloak stores usernames in lower case, so a differently cased username is the same user
    /// </summary>
    [Fact]
    public async Task GetRoleChanges_DifferentlyCasedUsername_IsTheSameUser()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice");
        var before = new[] { User("alice", AdministrationRole.FinanceReviewer) };
        var after = new[] { User("Alice", AdministrationRole.FinanceReviewer, AdministrationRole.HeadFinanceOfficer) };

        // Act
        var result = await GetRoleChanges(before, after);

        // Assert
        var change = Assert.Single(result.Value);
        Assert.Equal(Change("alice", AdministrationRole.HeadFinanceOfficer, RoleChangeType.Added), change);
    }

    [Fact]
    public async Task GetRoleChanges_SeveralChangesOfTheSameUserAndRole_LoadsEachUserAndGroupOnce()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice", "bob");
        var before = new[] { User("alice", AdministrationRole.FinanceAdministrator) };
        var after = new[]
        {
            User("alice", AdministrationRole.FinanceReviewer),
            User("bob", AdministrationRole.FinanceReviewer, AdministrationRole.FinanceAdministrator),
        };

        // Act
        var result = await GetRoleChanges(before, after);

        // Assert
        Assert.Equal(4, result.Value.Length);
        await _keycloakUserClient.Received(1).GetUserByUsername("alice", Arg.Any<CancellationToken>());
        await _keycloakUserClient.Received(1).GetUserByUsername("bob", Arg.Any<CancellationToken>());
        await _keycloakUserClient.Received(2).GetGroupIdFromPath(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRoleChanges_UnknownUser_ReturnsUnknownUserError()
    {
        // Arrange
        _keycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));

        // Act
        var result = await GetRoleChanges([], [User("mallory", AdministrationRole.FinanceAdministrator)]);

        // Assert
        Assert.Equal(new UnknownUserError("mallory"), result.Error);
    }

    [Fact]
    public async Task GetRoleChanges_RoleGroupMissing_ReturnsLoadCurrentUserRolesError()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice");
        var headGroup = KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer);
        var groupError = new NotFoundError("Group", headGroup);
        _keycloakUserClient.GetGroupIdFromPath(headGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(groupError));

        // Act
        var result = await GetRoleChanges([], [User("alice", AdministrationRole.HeadFinanceOfficer)]);

        // Assert
        Assert.Equal(new LoadCurrentUserRolesError(groupError), result.Error);
    }

    [Fact]
    public async Task ExecuteRoleChanges_NoChanges_ChangesNothing()
    {
        // Act
        var result = await MyTask.ExecuteRoleChanges(Array.Empty<RoleChange<AdministrationRole>>(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_keycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task ExecuteRoleChanges_Changes_AppliesThemInOrder()
    {
        // Arrange
        _keycloakUserClient.SetupUsers();
        var changes = new[]
        {
            Change("alice", AdministrationRole.FinanceAdministrator, RoleChangeType.Removed),
            Change("bob", AdministrationRole.FinanceReviewer, RoleChangeType.Added),
        };

        // Act
        var result = await MyTask.ExecuteRoleChanges(changes, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Received.InOrder(() =>
        {
            _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceAdministrator), CancellationToken.None);
            _keycloakUserClient.AddUserToGroup("id-bob", RoleGroupId(AdministrationRole.FinanceReviewer), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteRoleChanges_ChangeFails_ReturnsChangeUserRoleErrorAndStops()
    {
        // Arrange
        _keycloakUserClient.SetupUsers();
        var keycloakError = new NotFoundError("User", "id-alice");
        _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceAdministrator), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(keycloakError));
        var changes = new[]
        {
            Change("alice", AdministrationRole.FinanceAdministrator, RoleChangeType.Removed),
            Change("bob", AdministrationRole.FinanceReviewer, RoleChangeType.Added),
        };

        // Act
        var result = await MyTask.ExecuteRoleChanges(changes, CancellationToken.None);

        // Assert
        Assert.Equal(
            new ChangeUserRoleError("alice", nameof(AdministrationRole.FinanceAdministrator), Added: false, keycloakError),
            result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
