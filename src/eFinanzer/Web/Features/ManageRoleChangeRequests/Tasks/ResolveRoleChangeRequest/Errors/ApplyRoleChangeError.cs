using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;

/// <summary>
/// Applying a role change failed. Role changes before the failed one were already applied in Keycloak, but the request
/// stays unresolved, so approving it again applies the rest.
/// </summary>
public record ApplyRoleChangeError(int Id, string AccountingUnitKey, string UserName, AccountingUnitRole Role,
    bool IsAdded, Error ParentError)
    : Error($"Could not {(IsAdded ? "add" : "remove")} the role {Role} {(IsAdded ? "to" : "from")} user '{UserName}' " +
            $"in accounting unit '{AccountingUnitKey}' of role change request '{Id}'. Role changes before this one " +
            "were applied, the request stays unresolved.", ParentError);
