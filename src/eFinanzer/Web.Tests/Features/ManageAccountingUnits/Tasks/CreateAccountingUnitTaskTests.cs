using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class CreateAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private CreateAccountingUnitTask MyTask => new(Database.CreateContext(), NullLogger<CreateAccountingUnitTask>.Instance);

    [Fact]
    public async Task Create_ValidCommand_StoresAccountingUnit()
    {
        // Arrange
        var command = new CreateAccountingUnitCommand
        {
            Key = "fsr-inf_1",
            Name = "Fachschaftsrat Informatik",
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
            Name = "Fachschaftsrat Informatik",
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
}
