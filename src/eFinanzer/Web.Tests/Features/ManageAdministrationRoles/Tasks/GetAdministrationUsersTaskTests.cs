using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Errors;
using eFinanzer.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAdministrationRoles.Tasks;

public class GetAdministrationUsersTaskTests
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private GetAdministrationUsersTask MyTask => new(_keycloakUserClient, NullLogger<GetAdministrationUsersTask>.Instance);

    [Fact]
    public async Task GetUsers_UsersInRoleGroups_ReturnsUsersOrderedWithTheirRoles()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers(
            (AdministrationRole.FinanceReviewer, ["bob", "alice"]),
            (AdministrationRole.HeadFinanceOfficer, ["alice"]));

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value.AdministrationUsers,
            alice =>
            {
                Assert.Equal("alice", alice.Username);
                Assert.Equal([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceReviewer], alice.Roles);
            },
            bob =>
            {
                Assert.Equal("bob", bob.Username);
                Assert.Equal([AdministrationRole.FinanceReviewer], bob.Roles);
            });
    }

    [Fact]
    public async Task GetUsers_NoRoleGroups_ReturnsNoUsers()
    {
        // Arrange
        _keycloakUserClient.SetupAdministrationRoleMembers();

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AdministrationUsers);
    }

    [Fact]
    public async Task GetUsers_SubgroupIsNoKnownRole_IgnoresSubgroup()
    {
        // Arrange
        _keycloakUserClient.SetupGroups(KeycloakGroups.AdministrationGroup);
        _keycloakUserClient.GetSubgroups(KeycloakUserClientExtensions.GroupId(KeycloakGroups.AdministrationGroup), Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Ok([KeycloakUserClientExtensions.Group("/administration/auditor")]));

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AdministrationUsers);
        await _keycloakUserClient.DidNotReceive().GetUsersInGroup(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUsers_AdministrationGroupMissing_ReturnsLoadRoleGroupsError()
    {
        // Arrange
        var keycloakError = new NotFoundError("Group", KeycloakGroups.AdministrationGroup);
        _keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.AdministrationGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(keycloakError));

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAdministrationRoleGroupsError(keycloakError), result.Error);
    }

    [Fact]
    public async Task GetUsers_LoadingSubgroupsFails_ReturnsLoadRoleGroupsError()
    {
        // Arrange
        var groupId = KeycloakUserClientExtensions.GroupId(KeycloakGroups.AdministrationGroup);
        var keycloakError = new NotFoundError("Group", groupId);
        _keycloakUserClient.SetupGroups(KeycloakGroups.AdministrationGroup);
        _keycloakUserClient.GetSubgroups(groupId, Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Fail(keycloakError));

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAdministrationRoleGroupsError(keycloakError), result.Error);
    }

    [Fact]
    public async Task GetUsers_LoadingRoleMembersFails_ReturnsLoadRoleMembersError()
    {
        // Arrange
        var group = KeycloakUserClientExtensions.AdministrationRoleGroup(AdministrationRole.FinanceAdministrator);
        var keycloakError = new NotFoundError("Group", group.Path);
        _keycloakUserClient.SetupAdministrationRoleMembers((AdministrationRole.FinanceAdministrator, []));
        _keycloakUserClient.GetUsersInGroup(group.Id, Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakUser>>.Fail(keycloakError));

        // Act
        var result = await MyTask.GetUsers(CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAdministrationRoleMembersError(group.Path, keycloakError), result.Error);
    }
}
