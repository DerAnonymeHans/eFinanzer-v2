using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Tests.Features.ManageFiscalYears.Tasks;

public class CreateFiscalYearTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private CreateFiscalYearTask MyTask => new(Database.CreateContext());

    private static CreateFiscalYearCommand ValidCommand => new()
    {
        Key = "2026",
        AccountingUnitKey = "fsr",
        Name = "HJ 2026",
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(new AccountingUnit { Key = "fsr" }, new AccountingUnit { Key = "stura" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ValidCommand_StoresFiscalYear()
    {
        // Act
        var result = await MyTask.Create(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await Context.FiscalYears.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(new FiscalYear
        {
            Key = "2026",
            AccountingUnitKey = "fsr",
            Name = "HJ 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        }, stored);
    }

    [Fact]
    public async Task Create_KeyExistsInOtherUnit_StoresFiscalYear()
    {
        // Arrange
        Context.FiscalYears.Add(new FiscalYear
        {
            Key = "2026",
            AccountingUnitKey = "stura",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Create(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await Context.FiscalYears.CountAsync(x => x.Key == "2026", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_ReturnsConflictAndKeepsExistingFiscalYear()
    {
        // Arrange
        Context.FiscalYears.Add(new FiscalYear
        {
            Key = "2026",
            AccountingUnitKey = "fsr",
            Name = "Original",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.Create(ValidCommand, CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearAlreadyExistsError("2026", "fsr"), result.Error);
        var stored = await Context.FiscalYears.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Original", stored.Name);
    }

    [Fact]
    public async Task Create_UnknownAccountingUnit_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Create(ValidCommand with { AccountingUnitKey = "unknown" }, CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
        Assert.False(await Context.FiscalYears.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678901")]
    [InlineData("20 26")]
    public async Task Create_InvalidKey_ReturnsInvalidInput(string key)
    {
        // Act
        var result = await MyTask.Create(ValidCommand with { Key = key }, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Key", error.Field);
    }

    [Fact]
    public async Task Create_NameTooLong_ReturnsInvalidInput()
    {
        // Act
        var result = await MyTask.Create(ValidCommand with { Name = "Haushaltsjahr 2026" }, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal("Name", error.Field);
    }

    public static TheoryData<DateOnly, DateOnly, string> InvalidDates => new()
    {
        // End before start
        { new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1), "StartDate" },
        // Same day
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), "StartDate" },
        // Before the minimum date
        { new DateOnly(1899, 1, 1), new DateOnly(1899, 12, 31), "StartDate" },
        // Shorter than the minimum length
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), "EndDate" },
        // One day shorter than the minimum length of 100 days
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 9), "EndDate" },
    };

    [Theory]
    [MemberData(nameof(InvalidDates))]
    public async Task Create_InvalidDates_ReturnsInvalidInput(DateOnly startDate, DateOnly endDate, string field)
    {
        // Act
        var result = await MyTask.Create(ValidCommand with { StartDate = startDate, EndDate = endDate }, CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal(field, error.Field);
        Assert.False(await Context.FiscalYears.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_ExactlyMinimumLength_StoresFiscalYear()
    {
        // Act: 2026-01-01 to 2026-04-10 are 100 days including both ends
        var result = await MyTask.Create(
            ValidCommand with { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 10) },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    private async Task AddExistingFiscalYear(string accountingUnitKey)
    {
        Context.FiscalYears.Add(new FiscalYear
        {
            Key = "existing",
            AccountingUnitKey = accountingUnitKey,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Periods overlapping the existing fiscal year from 2026-01-01 to 2026-12-31
    /// </summary>
    public static TheoryData<DateOnly, DateOnly> OverlappingPeriods => new()
    {
        // Same period
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31) },
        // Inside the existing one
        { new DateOnly(2026, 2, 1), new DateOnly(2026, 11, 30) },
        // Surrounding the existing one
        { new DateOnly(2025, 12, 1), new DateOnly(2027, 1, 31) },
        // Starting inside the existing one
        { new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30) },
        // Ending inside the existing one
        { new DateOnly(2025, 7, 1), new DateOnly(2026, 6, 30) },
        // Starting on the last day of the existing one
        { new DateOnly(2026, 12, 31), new DateOnly(2027, 12, 30) },
        // Ending on the first day of the existing one
        { new DateOnly(2025, 1, 2), new DateOnly(2026, 1, 1) },
    };

    [Theory]
    [MemberData(nameof(OverlappingPeriods))]
    public async Task Create_OverlapsExistingFiscalYear_ReturnsConflict(DateOnly startDate, DateOnly endDate)
    {
        // Arrange
        await AddExistingFiscalYear("fsr");

        // Act
        var result = await MyTask.Create(ValidCommand with { StartDate = startDate, EndDate = endDate }, CancellationToken.None);

        // Assert
        Assert.Equal(new FiscalYearOverlapsExistingError("2026", "fsr", startDate, endDate,
            "existing", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), result.Error);
        Assert.Equal(1, await Context.FiscalYears.CountAsync(TestContext.Current.CancellationToken));
    }

    public static TheoryData<DateOnly, DateOnly> AdjacentPeriods => new()
    {
        { new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31) },
        { new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31) },
    };

    [Theory]
    [MemberData(nameof(AdjacentPeriods))]
    public async Task Create_AdjacentToExistingFiscalYear_StoresFiscalYear(DateOnly startDate, DateOnly endDate)
    {
        // Arrange
        await AddExistingFiscalYear("fsr");

        // Act
        var result = await MyTask.Create(ValidCommand with { StartDate = startDate, EndDate = endDate }, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, await Context.FiscalYears.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_OverlapsFiscalYearOfOtherUnit_StoresFiscalYear()
    {
        // Arrange
        await AddExistingFiscalYear("stura");

        // Act
        var result = await MyTask.Create(ValidCommand, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
