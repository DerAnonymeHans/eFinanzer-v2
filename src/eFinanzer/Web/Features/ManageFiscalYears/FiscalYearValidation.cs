using System.Text.RegularExpressions;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;

namespace eFinanzer.Features.ManageFiscalYears;

/// <summary>
/// Input constraints of an accounting unit, shared by the create and update tasks
/// </summary>
public static partial class FiscalYearValidation
{
    public const int KeyMaxLength = 10;
    public const int NameMaxLength = 10;
    private static readonly DateOnly MinDate = new DateOnly(1900, 01, 01);
    private static readonly TimeSpan MinFiscalYear = TimeSpan.FromDays(100);

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex KeyRegex();

    public static Result ValidateKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > KeyMaxLength)
            return new InvalidInputError("Key", $"Key must be between 1 and {KeyMaxLength} characters long");

        if (!KeyRegex().IsMatch(key))
            return new InvalidInputError("Key", "Key may only contain the characters a-z, A-Z, 0-9, '_' and '-'");

        return Result.Ok();
    }

    public static Result ValidateName(string? name)
    {
        if (name?.Length > NameMaxLength)
            return new InvalidInputError("Name", $"Name must not be longer than {NameMaxLength} characters");

        return Result.Ok();
    }
    
    public static Result ValidateDates(DateOnly start, DateOnly end)
    {
        if (start >= end)
            return new InvalidInputError("StartDate", $"StartDate must be less than endDate");
        
        if (start < MinDate)
            return new InvalidInputError("StartDate", $"StartDate must be greater than {MinDate}");

        if (end.ToDateTime(TimeOnly.MaxValue) - start.ToDateTime(TimeOnly.MinValue) < MinFiscalYear)
            return new InvalidInputError("EndDate", $"EndDate must be more than {MinFiscalYear} after startDate");
        
        return Result.Ok();
    }

    /// <summary>
    /// Fiscal years sharing at least one day with the period from <paramref name="start"/> to <paramref name="end"/>.
    /// Both dates are inclusive, so a fiscal year ending on the day another one starts overlaps it.
    /// </summary>
    public static IQueryable<FiscalYear> WhereOverlaps(this IQueryable<FiscalYear> fiscalYears, DateOnly start, DateOnly end)
        => fiscalYears.Where(year => year.StartDate <= end && year.EndDate >= start);
}
