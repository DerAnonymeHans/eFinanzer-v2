using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class CreateAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private CreateAccountingUnitTask MyTask => new(Database.CreateContext(), _keycloakUserClient, NullLogger<CreateAccountingUnitTask>.Instance);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _keycloakUserClient.SetupGroupCreation();
    }

    [Fact]
    public async Task Create_ValidCommand_StoresAccountingUnit()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand
        {
            Key = "fsr-inf_1",
            Name = "FSR Informatik",
            Description = "Finanzen des FSR",
        };

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await Context.AccountingUnits.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new AccountingUnit
        {
            Key = "fsr-inf_1",
            Name = "FSR Informatik",
            Description = "Finanzen des FSR",
        }, stored);
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_ReturnsConflictAndKeepsExistingUnit()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "Original" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Create(new CreateAccountingUnitCommand { Key = "fsr", Name = "Duplicate" }, CancellationToken.None);

        // Assert
        Assert.Equal(new AccountingUnitKeyAlreadyExistsError("fsr"), result.Error);
        var stored = await Context.AccountingUnits.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Original", stored.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcdefghijk")]
    [InlineData("fsr inf")]
    [InlineData("fsr/inf")]
    [InlineData("räte")]
    public async Task Create_InvalidKey_ReturnsInvalidInputAndStoresNothing(string key)
    {
        // Arrange
        var command = new CreateAccountingUnitCommand { Key = key };

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Key", error.Field);
        Assert.False(await Context.AccountingUnits.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_KeyWithMaxLength_StoresAccountingUnit()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand { Key = "abcdefghij" };

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(await Context.AccountingUnits.AnyAsync(x => x.Key == "abcdefghij", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_NameTooLong_ReturnsInvalidInput()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand { Key = "fsr", Name = new string('a', 51) };

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Name", error.Field);
    }

    [Fact]
    public async Task Create_DescriptionTooLong_ReturnsInvalidInput()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand { Key = "fsr", Description = new string('a', 501) };

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Description", error.Field);
    }

    [Fact]
    public async Task Create_ValidCommand_CreatesUnitGroupWithOneSubgroupPerRole()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand { Key = "fsr" };
        var unitGroupId = KeycloakUserClientExtensions.GroupId(KeycloakGroups.GetAccountingUnitGroup("fsr"));

        // Act
        var result = await MyTask.Create(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _keycloakUserClient.Received(1).CreateSubgroup(
            KeycloakUserClientExtensions.GroupId(KeycloakGroups.AccountingUnitsGroup), "fsr", Arg.Any<CancellationToken>());
        foreach (var role in Enum.GetValues<AccountingUnitRole>())
        {
            await _keycloakUserClient.Received(1).CreateSubgroup(
                unitGroupId, KeycloakGroups.GetGroupName(role), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Create_GroupsAlreadyExist_ReusesThemAndStoresAccountingUnit()
    {
        // Arrange
        var unitGroupPath = KeycloakGroups.GetAccountingUnitGroup("fsr");
        _keycloakUserClient.CreateSubgroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<string>.Fail(new AlreadyExistsError("Group", call.ArgAt<string>(1))));
        _keycloakUserClient.SetupGroups(Enum.GetValues<AccountingUnitRole>()
            .Select(role => KeycloakGroups.GetAccountingUnitRoleGroup("fsr", role))
            .Append(unitGroupPath)
            .ToArray());

        // Act
        var result = await MyTask.Create(new CreateAccountingUnitCommand { Key = "fsr" }, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(await Context.AccountingUnits.AnyAsync(x => x.Key == "fsr", TestContext.Current.CancellationToken));
        await _keycloakUserClient.Received(Enum.GetValues<AccountingUnitRole>().Length).CreateSubgroup(
            KeycloakUserClientExtensions.GroupId(unitGroupPath), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_CreatingGroupsFails_ReturnsCreateGroupsErrorAndStoresNothing()
    {
        // Arrange
        var keycloakError = new NotFoundError("Group", KeycloakGroups.AccountingUnitsGroup);
        _keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.AccountingUnitsGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(keycloakError));

        // Act
        var result = await MyTask.Create(new CreateAccountingUnitCommand { Key = "fsr" }, CancellationToken.None);

        // Assert
        Assert.Equal(new CreateAccountingUnitGroupsError("fsr", keycloakError), result.Error);
        Assert.False(await Context.AccountingUnits.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_DoesNotCreateGroups()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await MyTask.Create(new CreateAccountingUnitCommand { Key = "fsr" }, CancellationToken.None);

        // Assert
        await _keycloakUserClient.DidNotReceive().CreateSubgroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
