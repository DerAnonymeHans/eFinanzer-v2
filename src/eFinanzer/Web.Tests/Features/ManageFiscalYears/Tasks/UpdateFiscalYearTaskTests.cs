using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear;
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
}
