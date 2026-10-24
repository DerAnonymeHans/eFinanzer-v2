namespace eFinanzer.Common.Tasks.GetRoleChanges.Errors;

/// <summary>
/// Changing a role failed. Role changes before the failed one were already applied in Keycloak.
/// </summary>
/// <param name="Role">Name of the role. A string, because the JSON serialization cannot write a value typed as <see cref="Enum"/>.</param>
public record ChangeUserRoleError(string UserName, string Role, bool Added, Error ParentError)
    : Error($"Could not {(Added ? "add" : "remove")} the role {Role} {(Added ? "to" : "from")} user '{UserName}'. Role changes before this one were applied.", ParentError);
