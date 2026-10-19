namespace eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Errors;

public record FiscalYearWithBookingsShortenedError(
    string Key,
    string AccountingUnitKey,
    DateOnly StartDate,
    DateOnly EndDate)
    : Error($"Fiscal year '{Key}' of accounting unit '{AccountingUnitKey}' has receipts outside " +
            $"the new period {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
