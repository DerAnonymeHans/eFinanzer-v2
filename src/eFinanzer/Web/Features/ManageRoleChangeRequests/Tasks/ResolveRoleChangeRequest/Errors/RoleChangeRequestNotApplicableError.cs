namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;

/// <summary>
/// The requesting user or a requested role group no longer exists in Keycloak, so the request can only be dismissed
/// </summary>
public record RoleChangeRequestNotApplicableError(int Id, string Reason, Error ParentError)
    : Error($"Role change request '{Id}' cannot be approved: {Reason}", ParentError);
