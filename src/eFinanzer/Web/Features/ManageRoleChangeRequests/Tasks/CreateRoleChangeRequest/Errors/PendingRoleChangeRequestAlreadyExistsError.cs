namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Errors;

public record PendingRoleChangeRequestAlreadyExistsError() 
    : Error("You already have a pending role change request. Delete it before creating a new one.");