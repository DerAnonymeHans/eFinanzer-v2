namespace eFinanzer.Features.ManageRoleChangeRequests.Errors;

public record RoleChangeRequestAlreadyResolvedError(int Id)
    : Error($"Role change request '{Id}' was already resolved");
