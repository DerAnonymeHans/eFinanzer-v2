namespace eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;

public record DeleteAccountingUnitLogoError(string Key, Error ParentError)
    : Error($"Could not delete the logo of accounting unit '{Key}'", ParentError);
