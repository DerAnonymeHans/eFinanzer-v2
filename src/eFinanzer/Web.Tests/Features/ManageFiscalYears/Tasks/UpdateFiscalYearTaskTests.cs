using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eFinanzer.Tests.Features.ManageFiscalYears.Tasks;

public class UpdateFiscalYearTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private UpdateFiscalYearTask MyTask => new(Database.CreateContext(), NullLogger<UpdateFiscalYearTask>.Instance);

    private static FiscalYear Original(string accountingUnitKey) => new()
    {
        Key = "2026",
        AccountingUnitKey = accountingUnitKey,
        Name = "Original",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
    };

    private static UpdateFiscalYearCommand ValidCommand => new()
    {
        Key = "2026",
        AccountingUnitKey = "fsr",
        Name = "HJ 26/27",
        StartDate = new DateOnly(2026, 4, 1),
        EndDate = new DateOnly(2027, 3, 31),
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new AccountingUnit { Key = "stura" },
            Original("fsr"),
            Original("stura"));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Update_ValidCommand_UpdatesOnlyThisFiscalYear()
    {
        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await Context.FiscalYears
            .OrderBy(x => x.AccountingUnitKey)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([
            new FiscalYear
            {
                Key = "2026",
                AccountingUnitKey = "fsr",
                Name = "HJ 26/27",
                StartDate = new DateOnly(2026, 4, 1),
                EndDate = new DateOnly(2027, 3, 31),
            },
            Original("stura"),
        ], stored);
    }

    [Fact]
    public async Task Update_NameNull_RemovesName()
    {
        // Act
        var result = await MyTask.Update(ValidCommand with { Name = null }, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await Context.FiscalYears.SingleAsync(x => x.AccountingUnitKey == "fsr", TestContext.Current.CancellationToken);
        Assert.Null(stored.Name);
    }

    [Theory]
    [InlineData("fsr", "unknown")]
    [InlineData("unknown", "2026")]
    public async Task Update_UnknownFiscalYear_ReturnsNotFound(string accountingUnitKey, string key)
    {
        // Act
        var result = await MyTask.Update(ValidCommand with { AccountingUnitKey = accountingUnitKey, Key = key }, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(FiscalYear), $"{accountingUnitKey}/{key}"), result.Error);
    }

    [Fact]
    public async Task Update_NameTooLong_ReturnsInvalidInputAndKeepsFiscalYear()
    {
        // Act
        var result = await MyTask.Update(ValidCommand with { Name = "Haushaltsjahr 2026" }, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Name", error.Field);
        var stored = await Context.FiscalYears.SingleAsync(x => x.AccountingUnitKey == "fsr", TestContext.Current.CancellationToken);
        Assert.Equal(Original("fsr"), stored);
    }

    [Fact]
    public async Task Update_EndBeforeStart_ReturnsInvalidInputAndKeepsFiscalYear()
    {
        // Act
        var result = await MyTask.Update(
            ValidCommand with { StartDate = new DateOnly(2027, 3, 31), EndDate = new DateOnly(2026, 4, 1) },
            CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("StartDate", error.Field);
        var stored = await Context.FiscalYears.SingleAsync(x => x.AccountingUnitKey == "fsr", TestContext.Current.CancellationToken);
        Assert.Equal(Original("fsr"), stored);
    }

    [Fact]
    public async Task Update_OverlapsOwnOldPeriod_UpdatesFiscalYear()
    {
        // Act
        var result = await MyTask.Update(
            ValidCommand with { StartDate = new DateOnly(2026, 2, 1), EndDate = new DateOnly(2026, 12, 31) },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_OverlapsOtherFiscalYear_ReturnsConflictAndKeepsFiscalYear()
    {
        // Arrange
        Context.FiscalYears.Add(new FiscalYear
        {
            Key = "2027",
            AccountingUnitKey = "fsr",
            StartDate = new DateOnly(2027, 1, 1),
            EndDate = new DateOnly(2027, 12, 31),
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearOverlapsExistingError("2026", "fsr", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31),
            "2027", new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31)), result.Error);
        var stored = await Context.FiscalYears.SingleAsync(x => x.AccountingUnitKey == "fsr" && x.Key == "2026",
            TestContext.Current.CancellationToken);
        Assert.Equal(Original("fsr"), stored);
    }

    [Fact]
    public async Task Update_UnknownFiscalYearOverlappingOther_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Update(
            ValidCommand with { Key = "unknown", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) },
            CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(FiscalYear), "fsr/unknown"), result.Error);
    }

    private async Task AddReceipt(string accountingUnitKey, DateOnly receiptAt)
    {
        Context.Receipts.Add(new Receipt
        {
            Key = $"R-{receiptAt:yyyyMMdd}",
            AccountingUnitKey = accountingUnitKey,
            FiscalYearKey = "2026",
            ReceiptAt = receiptAt,
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Periods of the update command from 2026-04-01 to 2027-03-31 not containing the receipt date
    /// </summary>
    public static TheoryData<DateOnly> ReceiptDatesOutsideNewPeriod => new()
    {
        new DateOnly(2026, 3, 31),
        new DateOnly(2027, 4, 1),
    };

    [Theory]
    [MemberData(nameof(ReceiptDatesOutsideNewPeriod))]
    public async Task Update_ReceiptWouldLeavePeriod_ReturnsConflictAndKeepsFiscalYear(DateOnly receiptAt)
    {
        // Arrange
        await AddReceipt("fsr", new DateOnly(2026, 6, 1));
        await AddReceipt("fsr", receiptAt);

        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearWithBookingsShortenedError("2026", "fsr", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)),
            result.Error);
        var stored = await Context.FiscalYears.SingleAsync(x => x.AccountingUnitKey == "fsr", TestContext.Current.CancellationToken);
        Assert.Equal(Original("fsr"), stored);
    }

    public static TheoryData<DateOnly> ReceiptDatesInsideNewPeriod => new()
    {
        // First day
        new DateOnly(2026, 4, 1),
        // Last day
        new DateOnly(2026, 12, 31),
    };

    [Theory]
    [MemberData(nameof(ReceiptDatesInsideNewPeriod))]
    public async Task Update_ShortenedButReceiptsStayInside_UpdatesFiscalYear(DateOnly receiptAt)
    {
        // Arrange
        await AddReceipt("fsr", receiptAt);

        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_ShortenedWithBudgets_UpdatesFiscalYear()
    {
        // Arrange
        Context.Budgets.Add(new Budget { AccountingUnitKey = "fsr", FiscalYearKey = "2026" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Update_OtherUnitHasReceiptOutsideNewPeriod_UpdatesFiscalYear()
    {
        // Arrange
        await AddReceipt("stura", new DateOnly(2026, 1, 15));

        // Act
        var result = await MyTask.Update(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
