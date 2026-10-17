using System.Net;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;

public record AccountingUnitHasFiscalYearsError(string Key)
    : Error($"Accounting unit '{Key}' still has fiscal years and cannot be deleted", HttpStatusCode.Conflict);
