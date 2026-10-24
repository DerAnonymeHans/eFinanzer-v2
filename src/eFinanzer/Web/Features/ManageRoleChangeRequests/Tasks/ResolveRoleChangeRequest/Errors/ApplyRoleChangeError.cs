namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;

/// <summary>
/// Applying a role change failed, see <see cref="Error.ParentError"/> for the failed change. Role changes before the
/// failed one were already applied in Keycloak, but the request stays unresolved, so approving it again applies the rest.
/// </summary>
public record ApplyRoleChangeError(int Id, Error ParentError)
    : Error($"Could not apply the role changes of role change request '{Id}'. Role changes before the failed one " +
            "were applied, the request stays unresolved.", ParentError);
