namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Errors;

public record LoadAccountingUnitRoleGroupsError(string Key, Error ParentError)
    : Error($"Could not load the role groups of accounting unit '{Key}' from Keycloak", ParentError);
