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

    private static string RoleGroup(AccountingUnitRole role) => KeycloakGroups.GetAccountingUnitRoleGroup("fsr", role);

    [Fact]
    public async Task Update_WithoutUsersWithRoles_UpdatesFieldsAndLeavesRolesUntouched()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Old name", Description = "Old description" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new UpdateAccountingUnitCommand { Key = "fsr", Name = "New name", Description = null };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new AccountingUnit { Key = "fsr", Name = "New name", Description = null }, await LoadAccountingUnit("fsr"));
        Assert.Empty(_keycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task Update_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        var command = new UpdateAccountingUnitCommand { Key = "unknown", Name = "Name" };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
    }

    [Fact]
    public async Task Update_UnknownKeyWithUsersWithRoles_ReturnsNotFoundWithoutChangingRoles()
    {
        // Arrange
        _keycloakUserClient.SetupUsers("alice");
        var command = new UpdateAccountingUnitCommand
        {
            Key = "unknown",
            UsersWithRoles = [new() { UserName = "alice", Roles = [AccountingUnitRole.Treasurer] }],
        };

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
        var command = new UpdateAccountingUnitCommand
        {
            Key = "fsr",
            UsersWithRoles =
            [
                // Keycloak stores usernames in lower case, so this is the existing user "alice"
                new() { UserName = "Alice", Roles = [AccountingUnitRole.FinanceOfficer] },
                new() { UserName = "carol", Roles = [AccountingUnitRole.Treasurer] },
            ],
        };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Received.InOrder(() =>
        {
            _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroup(AccountingUnitRole.Treasurer), CancellationToken.None);
            _keycloakUserClient.RemoveUserFromGroup("id-bob", RoleGroup(AccountingUnitRole.FinanceOfficer), CancellationToken.None);
            _keycloakUserClient.AddUserToGroup("id-carol", RoleGroup(AccountingUnitRole.Treasurer), CancellationToken.None);
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
        var command = new UpdateAccountingUnitCommand { Key = "fsr", UsersWithRoles = [] };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).RemoveUserFromGroup("id-alice", RoleGroup(AccountingUnitRole.Treasurer), Arg.Any<CancellationToken>());
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
        var command = new UpdateAccountingUnitCommand
        {
            Key = "fsr",
            Name = "New name",
            UsersWithRoles = [new() { UserName = "mallory", Roles = [AccountingUnitRole.Treasurer] }],
        };

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
        _keycloakUserClient.GetSubgroups(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Fail(new NotFoundError("Group", "/accounting-units/fsr")));
        var command = new UpdateAccountingUnitCommand { Key = "fsr", Name = "New name", UsersWithRoles = [] };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.IsType<LoadCurrentUserRolesError>(result.Error);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
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
        _keycloakUserClient.AddUserToGroup("id-alice", RoleGroup(AccountingUnitRole.Treasurer), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(keycloakError));
        var command = new UpdateAccountingUnitCommand
        {
            Key = "fsr",
            UsersWithRoles = [new() { UserName = "alice", Roles = [AccountingUnitRole.Treasurer] }],
        };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        Assert.Equal(new ChangeUserRoleError("fsr", "alice", AccountingUnitRole.Treasurer, Added: true, keycloakError), result.Error);
    }

    [Theory]
    [InlineData("alice", "Alice")]
    [InlineData("alice", " ")]
    public async Task Update_InvalidUsersWithRoles_ReturnsInvalidInputWithoutCallingKeycloak(string firstUserName, string secondUserName)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new UpdateAccountingUnitCommand
        {
            Key = "fsr",
            UsersWithRoles =
            [
                new() { UserName = firstUserName, Roles = [AccountingUnitRole.Treasurer] },
                new() { UserName = secondUserName, Roles = [AccountingUnitRole.FinanceOfficer] },
            ],
        };

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
        var command = new UpdateAccountingUnitCommand { Key = "fsr", Name = new string('a', 51) };

        // Act
        var result = await MyTask.Update(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Name", error.Field);
        Assert.Equal("Old name", (await LoadAccountingUnit("fsr")).Name);
    }
}
