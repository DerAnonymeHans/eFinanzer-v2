using System.Net;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;

public record LoadCurrentUserRolesError(string Key, Error ParentError)
    : Error($"Could not load the current user roles of accounting unit '{Key}'",
        HttpStatusCode.InternalServerError, ParentError);
