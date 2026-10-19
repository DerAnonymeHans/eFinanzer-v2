namespace eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;

public record CreateAccountingUnitGroupsError(string Key, Error ParentError)
    : Error($"Could not create the Keycloak groups of accounting unit '{Key}'", ParentError);
