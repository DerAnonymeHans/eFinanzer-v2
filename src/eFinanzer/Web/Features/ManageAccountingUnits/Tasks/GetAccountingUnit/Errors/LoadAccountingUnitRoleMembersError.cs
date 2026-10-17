using System.Net;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Errors;

public record LoadAccountingUnitRoleMembersError(string Key, string GroupPath, Error ParentError)
    : Error($"Could not load the members of group '{GroupPath}' of accounting unit '{Key}' from Keycloak",
        HttpStatusCode.InternalServerError, ParentError);
