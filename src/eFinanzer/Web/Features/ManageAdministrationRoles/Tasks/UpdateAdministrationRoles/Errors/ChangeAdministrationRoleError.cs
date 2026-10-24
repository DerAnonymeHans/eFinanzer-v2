using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;

/// <summary>
/// Changing a role failed. Role changes before the failed one were already applied in Keycloak.
/// </summary>
public record ChangeAdministrationRoleError(string UserName, AdministrationRole Role, bool Added, Error ParentError)
    : Error($"Could not {(Added ? "add" : "remove")} the administration role {Role} {(Added ? "to" : "from")} " +
            $"user '{UserName}'. Role changes before this one were applied.", ParentError);
