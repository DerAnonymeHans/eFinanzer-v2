using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class DeleteAccountingUnitTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private DeleteAccountingUnitTask MyTask => new(Database.CreateContext(), NullLogger<DeleteAccountingUnitTask>.Instance);

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
}
