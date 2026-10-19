using eFinanzer.Common.Database.Models;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Models;

public record ResolveRoleChangeRequestCommand
{
    public required int Id { get; init; }
    public required string ResolvedBy { get; init; }
    public required RoleChangeRequest.ResolutionStatus Resolution { get; init; }
    public string? Comment { get; init; }
}
