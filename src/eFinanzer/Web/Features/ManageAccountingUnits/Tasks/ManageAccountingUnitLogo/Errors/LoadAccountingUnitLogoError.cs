namespace eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;

public record LoadAccountingUnitLogoError(string Key, Error ParentError)
    : Error($"Could not load the logo of accounting unit '{Key}'", ParentError);
