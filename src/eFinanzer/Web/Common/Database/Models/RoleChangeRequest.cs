using eFinanzer.Common.Models;

namespace eFinanzer.Common.Database.Models;

public record RoleChangeRequest
{
    public int Id { get; init; }
    
    public required string RequestedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? RequestComment { get; init; }
    public required List<RoleChange> RoleChanges { get; init; }
    
    public string? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public ResolutionStatus? Resolution { get; set; }
    public string? ResolutionComment { get; set; }
    

    public enum ResolutionStatus
    {
        Dismissed,
        Approved
    }

    public record RoleChange
    {
        public required AccountingUnitRole AccountingUnitRole { get; init; }
        public required string AccountingUnitKey { get; init; }
        /// <summary>
        /// True - user requests to get the role, false - user requests to remove the role
        /// </summary>
        public bool IsAdded { get; init; }
    }
}