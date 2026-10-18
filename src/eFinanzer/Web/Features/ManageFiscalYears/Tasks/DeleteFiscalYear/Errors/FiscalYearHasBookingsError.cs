using System.Net;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear.Errors;

public record FiscalYearHasBookingsError(string Key, string AccountingUnitKey)
    : Error($"Fiscal year '{Key}' of accounting unit '{AccountingUnitKey}' still has budgets or receipts and cannot be deleted",
        HttpStatusCode.Conflict);
