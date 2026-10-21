using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests.Models;

public record GetRoleChangeRequestDto
{
    public required int Id { get; init; }
    public required string RequestedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? RequestComment { get; init; }
    public required RoleChangeDto[] RoleChanges { get; init; }

    public string? ResolvedBy { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public RoleChangeRequest.ResolutionStatus? Resolution { get; init; }
    public string? ResolutionComment { get; init; }

    public record RoleChangeDto
    {
        public required string AccountingUnitKey { get; init; }
        public required AccountingUnitRole AccountingUnitRole { get; init; }
        public required bool IsAdded { get; init; }
    }

    public static GetRoleChangeRequestDto FromEntity(RoleChangeRequest request) => new()
    {
        Id = request.Id,
        RequestedBy = request.RequestedBy,
        CreatedAt = request.CreatedAt,
        RequestComment = request.RequestComment,
        RoleChanges = request.RoleChanges
            .Select(x => new RoleChangeDto
            {
                AccountingUnitKey = x.AccountingUnitKey,
                AccountingUnitRole = x.AccountingUnitRole,
                IsAdded = x.IsAdded,
            })
            .ToArray(),
        ResolvedBy = request.ResolvedBy,
        ResolvedAt = request.ResolvedAt,
        Resolution = request.Resolution,
        ResolutionComment = request.ResolutionComment,
    };
}
