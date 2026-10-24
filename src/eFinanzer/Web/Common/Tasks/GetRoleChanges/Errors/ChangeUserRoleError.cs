namespace eFinanzer.Common.Tasks.GetRoleChanges.Errors;

/// <summary>
/// Changing a role failed. Role changes before the failed one were already applied in Keycloak.
/// </summary>
public record ChangeUserRoleError(string UserName, Enum Role, bool Added, Error ParentError)
    : Error($"Could not {(Added ? "add" : "remove")} the role {Role} {(Added ? "to" : "from")} user '{UserName}'. Role changes before this one were applied.", ParentError);
