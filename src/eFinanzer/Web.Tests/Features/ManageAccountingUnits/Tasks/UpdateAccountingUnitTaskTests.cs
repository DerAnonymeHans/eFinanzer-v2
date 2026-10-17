using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class UpdateAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private UpdateAccountingUnitTask MyTask => new(
        Database.CreateContext(),
        new GetAccountingUnitTask(Database.CreateContext(), _keycloakUserClient, NullLogger<GetAccountingUnitTask>.Instance),
        _keycloakUserClient,
        NullLogger<UpdateAccountingUnitTask>.Instance);

    private async Task<AccountingUnit> LoadAccountingUnit(string key)
        => await Context.AccountingUnits.SingleAsync(x => x.Key == key, TestContext.Current.CancellationToken);

    private static string RoleGroupId(AccountingUnitRole role) => KeycloakUserClientExtensions.RoleGroup("fsr", role).Id;

    private static UpdateAccountingUnitCommand Command(string key, params UpdateAccountingUnitCommand.UserRoles[] usersWithRoles) => new()
    {
        Key = key,
        Name = null,
        Description = null,
        UsersWithRoles = usersWithRoles.ToList(),
    };

    private static UpdateAccountingUnitCommand.UserRoles UserRoles(string userName, params AccountingUnitRole[] roles)
        => new() { UserName = userName, Roles = roles };

    [Fact]
    public async Task Update_UnchangedUsersWithRoles_UpdatesFieldsWithoutChangingRoles()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name", Description = "Old description" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr", (AccountingUnitRole.Treasurer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");
        var command = Command("fsr", UserRoles("alice", AccountingUnitRole.Treasurer)) with { Name = "New name" };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new AccountingUnit { Key = "fsr", Name = "New name", Description = null }, await LoadAccountingUnit("fsr"));
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownKey_ReturnsNotFoundWithoutChangingRoles()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice");
        var command = Command("unknown", UserRoles("alice", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ChangedUsersWithRoles_AppliesOnlyTheDifferenceWithRemovalsFirst()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr",
            (AccountingUnitRole.FinanceOfficer, ["alice", "bob"]),
            (AccountingUnitRole.Treasurer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice", "bob", "carol");
        var command = Command("fsr",
            // Keycloak stores usernames in lower case, so this is the existing user "alice"
            UserRoles("Alice", AccountingUnitRole.FinanceOfficer),
            UserRoles("carol", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Received.InOrder(() =>
        {
            _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId(AccountingUnitRole.Treasurer), CancellationToken.None);
            _keycloakUserClient.RemoveUserFromGroup("id-bob", RoleGroupId(AccountingUnitRole.FinanceOfficer), CancellationToken.None);
            _keycloakUserClient.AddUserToGroup("id-carol", RoleGroupId(AccountingUnitRole.Treasurer), CancellationToken.None);
        });
        await _keycloakUserClient.Received(1).AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.Received(2).RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_EmptyUsersWithRoles_RemovesAllRoles()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr", (AccountingUnitRole.Treasurer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");
        var command = Command("fsr");

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).RemoveUserFromGroup("id-alice", RoleGroupId(AccountingUnitRole.Treasurer), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownUser_ReturnsUnknownUserWithoutChangingAnything()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr", (AccountingUnitRole.Treasurer, ["alice"]));
        _keycloakUserClient.SetupUsers("alice");
        _keycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));
        var command = Command("fsr", UserRoles("mallory", AccountingUnitRole.Treasurer)) with { Name = "New name" };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.Equal(new UnknownUserError("mallory"), result.Error);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_LoadingCurrentRolesFails_ReturnsLoadCurrentUserRolesErrorWithoutChangingAnything()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.GetGroupIdFromPath(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", "/accounting-units/fsr")));
        var command = Command("fsr") with { Name = "New name" };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.IsType<LoadCurrentUserRolesError>(result.Error);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
    }

    [Fact]
    public async Task Update_RoleGroupMissing_ReturnsLoadCurrentUserRolesErrorWithoutChangingAnything()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr");
        _keycloakUserClient.SetupUsers("alice");
        var treasurerGroup = KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.Treasurer);
        _keycloakUserClient.GetGroupIdFromPath(treasurerGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", treasurerGroup)));
        var command = Command("fsr", UserRoles("alice", AccountingUnitRole.Treasurer)) with { Name = "New name" };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.IsType<LoadCurrentUserRolesError>(result.Error);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ChangingRoleFails_ReturnsChangeUserRoleError()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.SetupRoleMembers("fsr");
        _keycloakUserClient.SetupUsers("alice");
        var keycloakError = new NotFoundError("User", "id-alice");
        _keycloakUserClient.AddUserToGroup("id-alice", RoleGroupId(AccountingUnitRole.Treasurer), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(keycloakError));
        var command = Command("fsr", UserRoles("alice", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.Equal(new ChangeUserRoleError("fsr", "alice", AccountingUnitRole.Treasurer, Added: true, keycloakError), result.Error);
    }

    public static TheoryData<List<UpdateAccountingUnitCommand.UserRoles>?> InvalidUsersWithRoles()
    {
        var data = new TheoryData<List<UpdateAccountingUnitCommand.UserRoles>?>();
        data.Add(null!);
        data.Add([null!]);
        data.Add([UserRoles("alice", AccountingUnitRole.Treasurer), UserRoles("Alice", AccountingUnitRole.FinanceOfficer)]);
        data.Add([UserRoles("alice", AccountingUnitRole.Treasurer), UserRoles(" ", AccountingUnitRole.FinanceOfficer)]);
        data.Add([new() { UserName = "alice", Roles = null! }]);
        data.Add([UserRoles("alice", (AccountingUnitRole)99)]);
        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidUsersWithRoles))]
    public async Task Update_InvalidUsersWithRoles_ReturnsInvalidInputWithoutCallingKeycloak(
        List<UpdateAccountingUnitCommand.UserRoles>? usersWithRoles)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = Command("fsr") with { UsersWithRoles = usersWithRoles! };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal(nameof(UpdateAccountingUnitCommand.UsersWithRoles), error.Field);
        Assert.Empty(_keycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task Update_NameTooLong_ReturnsInvalidInputAndKeepsName()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = Command("fsr") with { Name = new string('a', 51) };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Name", error.Field);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
    }
}
