using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;

/// <summary>
/// Changing a role failed. Role changes before the failed one were already applied in Keycloak.
/// </summary>
public record ChangeUserRoleError(string Key, string UserName, AccountingUnitRole Role, bool Added, Error ParentError)
    : Error($"Could not {(Added ? "add" : "remove")} the role {Role} {(Added ? "to" : "from")} user '{UserName}' " +
            $"in accounting unit '{Key}'. Role changes before this one were applied.", ParentError);
