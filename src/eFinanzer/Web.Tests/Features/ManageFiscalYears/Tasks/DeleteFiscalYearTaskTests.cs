using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear.Errors;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eFinanzer.Tests.Features.ManageFiscalYears.Tasks;

public class DeleteFiscalYearTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private DeleteFiscalYearTask MyTask => new(Database.CreateContext(), NullLogger<DeleteFiscalYearTask>.Instance);

    private static FiscalYear CreateFiscalYear(string accountingUnitKey, string key) => new()
    {
        Key = key,
        AccountingUnitKey = accountingUnitKey,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new AccountingUnit { Key = "stura" },
            CreateFiscalYear("fsr", "2026"),
            CreateFiscalYear("fsr", "2027"),
            CreateFiscalYear("stura", "2026"));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ExistingFiscalYear_RemovesOnlyThisFiscalYear()
    {
        // Act
        var result = await MyTask.Delete("fsr", "2026", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var remaining = await Context.FiscalYears
            .OrderBy(x => x.AccountingUnitKey).ThenBy(x => x.Key)
            .Select(x => $"{x.AccountingUnitKey}/{x.Key}")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["fsr/2027", "stura/2026"], remaining);
    }

    [Theory]
    [InlineData("fsr", "unknown")]
    [InlineData("unknown", "2026")]
    public async Task Delete_UnknownFiscalYear_ReturnsNotFound(string accountingUnitKey, string key)
    {
        // Act
        var result = await MyTask.Delete(accountingUnitKey, key, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(FiscalYear), $"{accountingUnitKey}/{key}"), result.Error);
        Assert.Equal(3, await Context.FiscalYears.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_FiscalYearWithReceipts_ReturnsConflictAndKeepsFiscalYear()
    {
        // Arrange
        Context.Receipts.Add(new Receipt { Key = "R-1", AccountingUnitKey = "fsr", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", "2026", CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearHasBookingsError("2026", "fsr"), result.Error);
        Assert.True(await Context.FiscalYears.AnyAsync(x => x.AccountingUnitKey == "fsr" && x.Key == "2026",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_FiscalYearWithBudgets_ReturnsConflictAndKeepsFiscalYear()
    {
        // Arrange
        Context.Budgets.Add(new Budget { AccountingUnitKey = "fsr", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", "2026", CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearHasBookingsError("2026", "fsr"), result.Error);
        Assert.True(await Context.FiscalYears.AnyAsync(x => x.AccountingUnitKey == "fsr" && x.Key == "2026",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_OtherUnitHasBudgetsForSameKey_DeletesFiscalYear()
    {
        // Arrange
        Context.Budgets.Add(new Budget { AccountingUnitKey = "stura", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", "2026", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Delete_OtherUnitHasReceiptsForSameKey_DeletesFiscalYear()
    {
        // Arrange
        Context.Receipts.Add(new Receipt { Key = "R-1", AccountingUnitKey = "stura", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Delete("fsr", "2026", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
