using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class DeleteAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly string FsrGroupId = KeycloakUserClientExtensions.GroupId(KeycloakGroups.GetAccountingUnitGroup("fsr"));

    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private DeleteAccountingUnitTask MyTask => new(Database.CreateContext(), _keycloakUserClient, NullLogger<DeleteAccountingUnitTask>.Instance);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _keycloakUserClient.SetupGroups(KeycloakGroups.GetAccountingUnitGroup("fsr"));
        _keycloakUserClient.DeleteGroup(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Ok());
    }

    [Fact]
    public async Task Delete_ExistingUnit_RemovesOnlyThisUnit()
    {
        // Arrange
        Context.AddRange(new AccountingUnit { Key = "fsr" }, new AccountingUnit { Key = "stura" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var remainingKeys = await Context.AccountingUnits.Select(x => x.Key).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["stura"], remainingKeys);
    }

    [Fact]
    public async Task Delete_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        const string key = "unknown";

        // Act
        var result = await MyTask.Delete(key, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), key), result.Error);
    }

    [Fact]
    public async Task Delete_UnitWithFiscalYears_ReturnsConflictAndKeepsUnit()
    {
        // Arrange
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new FiscalYear
            {
                Key = "fsr-2026",
                AccountingUnitKey = "fsr",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
            });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new AccountingUnitHasFiscalYearsError("fsr"), result.Error);
        Assert.True(await Context.AccountingUnits.AnyAsync(x => x.Key == "fsr", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_ExistingUnit_DeletesItsKeycloakGroup()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).DeleteGroup(FsrGroupId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_KeycloakGroupMissing_DeletesUnit()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.GetAccountingUnitGroup("fsr"), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", KeycloakGroups.GetAccountingUnitGroup("fsr"))));

        // Act
        var result = await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(await Context.AccountingUnits.AnyAsync(TestContext.Current.CancellationToken));
        await _keycloakUserClient.DidNotReceive().DeleteGroup(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_DeletingKeycloakGroupFails_ReturnsDeleteGroupErrorAndKeepsUnit()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var keycloakError = new InvalidInputError("Group", "Keycloak refused");
        _keycloakUserClient.DeleteGroup(FsrGroupId, Arg.Any<CancellationToken>()).Returns(Result.Fail(keycloakError));

        // Act
        var result = await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new DeleteAccountingUnitGroupError("fsr", keycloakError), result.Error);
        Assert.True(await Context.AccountingUnits.AnyAsync(x => x.Key == "fsr", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_UnitWithFiscalYears_KeepsKeycloakGroup()
    {
        // Arrange
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new FiscalYear
            {
                Key = "fsr-2026",
                AccountingUnitKey = "fsr",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
            });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await MyTask.Delete("fsr", CancellationToken.None);

        // Assert
        await _keycloakUserClient.DidNotReceive().DeleteGroup(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
