namespace eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;

public record SaveAccountingUnitLogoError(string Key, Error ParentError)
    : Error($"Could not save the logo of accounting unit '{Key}'", ParentError);
