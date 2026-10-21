using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Models;

public record CreateRoleChangeRequestRequest
{
    public required string RequestedBy { get; init; }
    public string? Comment { get; init; }
    public required RoleChange[]  RoleChanges { get; init; }

    public record RoleChange
    {
        public required string AccountingUnitKey { get; init; }
        public required AccountingUnitRole AccountingUnitRole { get; init; }
        public bool IsAdded { get; init; }
    }
}