using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Features.ManageFiscalYears.Tasks;

public class GetFiscalYearsTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private GetFiscalYearsTask MyTask => new(Database.CreateContext());

    private static FiscalYear CreateFiscalYear(string accountingUnitKey, string key, int year) => new()
    {
        Key = key,
        AccountingUnitKey = accountingUnitKey,
        StartDate = new DateOnly(year, 1, 1),
        EndDate = new DateOnly(year, 12, 31),
    };

    [Fact]
    public async Task GetAll_ExistingUnit_ReturnsOnlyItsFiscalYearsNewestFirst()
    {
        // Arrange
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new AccountingUnit { Key = "stura" },
            CreateFiscalYear("fsr", "a", 2025),
            CreateFiscalYear("fsr", "c", 2027),
            CreateFiscalYear("fsr", "b", 2026),
            CreateFiscalYear("stura", "x", 2026));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.GetAll("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["c", "b", "a"], result.Value.Select(x => x.Key));
        Assert.All(result.Value, x => Assert.Equal("fsr", x.AccountingUnitKey));
    }

    [Fact]
    public async Task GetAll_FiscalYearWithReceipts_CountsOnlyItsReceipts()
    {
        // Arrange
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new AccountingUnit { Key = "stura" },
            CreateFiscalYear("fsr", "2026", 2026),
            CreateFiscalYear("stura", "2026", 2026),
            new Receipt { Key = "R-1", AccountingUnitKey = "fsr", FiscalYearKey = "2026" },
            new Receipt { Key = "R-2", AccountingUnitKey = "fsr", FiscalYearKey = "2026" },
            new Receipt { Key = "R-3", AccountingUnitKey = "stura", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.GetAll("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var fiscalYear = Assert.Single(result.Value);
        Assert.Equal(2, fiscalYear.ReceiptsCount);
        Assert.Equal(0, fiscalYear.BudgetsCount);
    }

    [Fact]
    public async Task GetAll_UnitWithoutFiscalYears_ReturnsEmptyList()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.GetAll("fsr", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetAll_UnknownUnit_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.GetAll("unknown", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
    }
}
