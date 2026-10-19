namespace eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;

public record DeleteAccountingUnitGroupError(string Key, Error ParentError)
    : Error($"Could not delete the Keycloak group of accounting unit '{Key}'", ParentError);
