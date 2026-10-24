namespace eFinanzer.Common.Tasks.GetRoleChanges.Models;

public record UserWithRoles<TRole>(string Username, TRole[]  Roles) where TRole : Enum;