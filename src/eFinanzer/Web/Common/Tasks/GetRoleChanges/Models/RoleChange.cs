using eFinanzer.Common.Models;

namespace eFinanzer.Common.Tasks.GetRoleChanges.Models;

public record RoleChange<TRole> where TRole : Enum
{
    public required string Username { get; init; }
    public required string UserId { get; init; }
    public required TRole Role { get; init; }
    public required string KeycloakGroupId { get; init; }
    public RoleChangeType Type { get; init; }
}

public enum RoleChangeType
{
    Added,
    Removed
}