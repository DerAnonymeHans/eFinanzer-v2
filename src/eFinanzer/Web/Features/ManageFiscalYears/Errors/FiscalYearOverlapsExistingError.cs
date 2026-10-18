using System.Net;

namespace eFinanzer.Features.ManageFiscalYears.Errors;

public record FiscalYearOverlapsExistingError(
    string Key,
    string AccountingUnitKey,
    DateOnly StartDate,
    DateOnly EndDate,
    string OverlappingKey,
    DateOnly OverlappingStartDate,
    DateOnly OverlappingEndDate)
    : Error($"Fiscal year '{Key}' ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}) of accounting unit '{AccountingUnitKey}' " +
            $"overlaps the existing fiscal year '{OverlappingKey}' ({OverlappingStartDate:yyyy-MM-dd} to {OverlappingEndDate:yyyy-MM-dd})",
        HttpStatusCode.Conflict);
