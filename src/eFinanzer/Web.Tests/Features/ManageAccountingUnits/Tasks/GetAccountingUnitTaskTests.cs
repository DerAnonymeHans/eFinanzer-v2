using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class GetAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private GetAccountingUnitTask MyTask => new(Database.CreateContext(), _keycloakUserClient, NullLogger<GetAccountingUnitTask>.Instance);

    [Fact]
    public async Task Get_ExistingUnit_ReturnsUnitWithUsersGroupedByRole()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "FSR", Description = "Fachschaftsrat" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr",
            (AccountingUnitRole.Treasurer, ["bob", "alice"]),
            (AccountingUnitRole.FinanceOfficer, ["alice"]));

        // Act
        var result = await MyTask.Get("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("fsr", result.Value.Key);
        Assert.Equal("FSR", result.Value.Name);
        Assert.Equal("Fachschaftsrat", result.Value.Description);
        Assert.Collection(result.Value.UsersWithRoles,
            alice =>
            {
                Assert.Equal("alice", alice.UserName);
                Assert.Equal([AccountingUnitRole.FinanceOfficer, AccountingUnitRole.Treasurer], alice.Roles);
            },
            bob =>
            {
                Assert.Equal("bob", bob.UserName);
                Assert.Equal([AccountingUnitRole.Treasurer], bob.Roles);
            });
    }

    [Fact]
    public async Task Get_SubgroupIsNoKnownRole_IgnoresSubgroup()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupSubgroups("fsr", KeycloakUserClientExtensions.Group("/accounting-units/fsr/auditor"));

        // Act
        var result = await MyTask.Get("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.UsersWithRoles);
        await _keycloakUserClient.DidNotReceive().GetUsersInGroup(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_UnknownKey_ReturnsNotFoundWithoutCallingKeycloak()
    {
        // Arrange
        const string key = "unknown";

        // Act
        var result = await MyTask.Get(key, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), key), result.Error);
        Assert.Empty(_keycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task Get_LoadingRoleGroupsFails_ReturnsLoadRoleGroupsError()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var keycloakError = new NotFoundError("Group", "/accounting-units/fsr");
        _keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.GetAccountingUnitGroup("fsr"), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(keycloakError));

        // Act
        var result = await MyTask.Get("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAccountingUnitRoleGroupsError("fsr", keycloakError), result.Error);
    }

    [Fact]
    public async Task Get_LoadingRoleMembersFails_ReturnsLoadRoleMembersError()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var group = KeycloakUserClientExtensions.RoleGroup("fsr", AccountingUnitRole.Treasurer);
        var keycloakError = new NotFoundError("Group", group.Path);
        _keycloakUserClient.SetupSubgroups("fsr", group);
        _keycloakUserClient.GetUsersInGroup(group.Id, Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakUser>>.Fail(keycloakError));

        // Act
        var result = await MyTask.Get("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAccountingUnitRoleMembersError("fsr", group.Path, keycloakError), result.Error);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUnitsOrderedByKey()
    {
        // Arrange
        Context.AddRange(
            new AccountingUnit { Key = "stura", Name = "StuRa" },
            new AccountingUnit { Key = "fsr", Name = "FSR", Description = "Fachschaftsrat" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.GetAll(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
        [
            new GetAccountingUnitManagementListDto { Key = "fsr", Name = "FSR", Description = "Fachschaftsrat" },
            new GetAccountingUnitManagementListDto { Key = "stura", Name = "StuRa" },
        ], result.Value);
    }

    [Fact]
    public async Task Get_LoadingSubgroupsFails_ReturnsLoadRoleGroupsError()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var groupPath = KeycloakGroups.GetAccountingUnitGroup("fsr");
        var keycloakError = new NotFoundError("Group", KeycloakUserClientExtensions.GroupId(groupPath));
        _keycloakUserClient.SetupGroups(groupPath);
        _keycloakUserClient.GetSubgroups(KeycloakUserClientExtensions.GroupId(groupPath), Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Fail(keycloakError));

        // Act
        var result = await MyTask.Get("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAccountingUnitRoleGroupsError("fsr", keycloakError), result.Error);
    }
}
