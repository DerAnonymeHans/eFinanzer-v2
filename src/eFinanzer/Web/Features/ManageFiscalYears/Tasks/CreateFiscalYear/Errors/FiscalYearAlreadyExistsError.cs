using System.Net;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Errors;

public record FiscalYearAlreadyExistsError(string Key, string AccountingUnitKey)
    : Error($"A fiscal year with the key '{Key}' for AccountingUnit {AccountingUnitKey} already exists", HttpStatusCode.Conflict);
