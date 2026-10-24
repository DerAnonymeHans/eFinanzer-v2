using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAdministrationRoles.Tasks;

public class UpdateAdministrationRolesTaskTests
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private UpdateAdministrationRolesTask MyTask => new(
        new GetAdministrationUsersTask(_keycloakUserClient, NullLogger<GetAdministrationUsersTask>.Instance),
        new HandleRoleChangesTask(_keycloakUserClient, NullLogger<HandleRoleChangesTask>.Instance),
        NullLogger<UpdateAdministrationRolesTask>.Instance);

    private static string RoleGroupId(AdministrationRole role) => KeycloakUserClientExtensions.AdministrationRoleGroup(role).Id;

    private static UpdateAdministrationRolesCommand Command(string userName, params AdministrationRole[] roles)
        => new() { Username = userName, AdministrationRoles = roles };

    [Fact]
    public async Task Update_UnchangedRoles_ChangesNothing()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers((AdministrationRole.FinanceAdministrator, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");

        // Act
        var result = await MyTask.Update(Command("alice", AdministrationRole.FinanceAdministrator), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.DidNotReceive().GetUserByUsername(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ChangedRoles_AppliesOnlyTheDifferenceWithRemovalsFirst()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers(
            (AdministrationRole.FinanceAdministrator, ["alice"]),
            (AdministrationRole.FinanceReviewer, ["alice", "bob"]));
        _keycloakUserClient.SetupUsers("alice", "bob");
        // Keycloak stores usernames in lower case, so this is the existing user "alice"
        var command = Command("Alice", AdministrationRole.FinanceReviewer, AdministrationRole.HeadFinanceOfficer);

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Received.InOrder(() =>
        {
            _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceAdministrator), CancellationToken.None);
            _keycloakUserClient.AddUserToGroup("id-alice", RoleGroupId(AdministrationRole.HeadFinanceOfficer), CancellationToken.None);
        });
        await _keycloakUserClient.Received(1).AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.Received(1).RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_EmptyRoles_RemovesAllRolesOfOnlyThisUser()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers(
            (AdministrationRole.FinanceAdministrator, ["alice", "bob"]),
            (AdministrationRole.FinanceReviewer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice", "bob");

        // Act
        var result = await MyTask.Update(Command("alice"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceAdministrator), Arg.Any<CancellationToken>());
        await _keycloakUserClient.Received(1).RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceReviewer), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().RemoveUserFromGroup("id-bob", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UserWithoutRoles_AddsRoles()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers();
        _keycloakUserClient.SetupUsers("carol");

        // Act
        var result = await MyTask.Update(Command("carol", AdministrationRole.FinanceReviewer), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).AddUserToGroup("id-carol", RoleGroupId(AdministrationRole.FinanceReviewer), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownUser_ReturnsUnknownUserWithoutChangingAnything()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers();
        _keycloakUserClient.SetupUsers();
        _keycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));

        // Act
        var result = await MyTask.Update(Command("mallory", AdministrationRole.HeadFinanceOfficer), CancellationToken.None);

        // Assert
        Assert.Equal(new UnknownUserError("mallory"), result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_LoadingCurrentRolesFails_ReturnsLoadCurrentRolesErrorWithoutChangingAnything()
    {
        // Arrange
        _keycloakUserClient.GetGroupIdFromPath(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", KeycloakGroups.AdministrationGroup)));

        // Act
        var result = await MyTask.Update(Command("alice", AdministrationRole.FinanceReviewer), CancellationToken.None);

        // Assert
        Assert.IsType<LoadCurrentAdministrationRolesError>(result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_RoleGroupMissing_ReturnsLoadCurrentRolesErrorWithoutChangingAnything()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers((AdministrationRole.FinanceReviewer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");
        var headGroup = KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer);
        _keycloakUserClient.GetGroupIdFromPath(headGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", headGroup)));

        // Act
        var result = await MyTask.Update(Command("alice", AdministrationRole.HeadFinanceOfficer), CancellationToken.None);

        // Assert
        Assert.IsType<LoadCurrentUserRolesError>(result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ChangingRoleFails_ReturnsChangeRoleErrorAndStops()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers((AdministrationRole.FinanceReviewer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");
        var keycloakError = new NotFoundError("User", "id-alice");
        _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId(AdministrationRole.FinanceReviewer), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(keycloakError));

        // Act
        var result = await MyTask.Update(Command("alice", AdministrationRole.FinanceAdministrator), CancellationToken.None);

        // Assert
        Assert.Equal(
            new ChangeUserRoleError("alice", nameof(AdministrationRole.FinanceReviewer), Added: false, keycloakError),
            result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<string, AdministrationRole[], string> InvalidCommands() => new()
    {
        { "", [AdministrationRole.FinanceReviewer], nameof(UpdateAdministrationRolesCommand.Username) },
        { " ", [], nameof(UpdateAdministrationRolesCommand.Username) },
        { "alice", [(AdministrationRole)99], nameof(UpdateAdministrationRolesCommand.AdministrationRoles) },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_InvalidCommand_ReturnsInvalidInputWithoutCallingKeycloak(
        string userName, AdministrationRole[] roles, string expectedField)
    {
        // Act
        var result = await MyTask.Update(Command(userName, roles), CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal(expectedField, error.Field);
        Assert.Empty(_keycloakUserClient.ReceivedCalls());
    }
}
