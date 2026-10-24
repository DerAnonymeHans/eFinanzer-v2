namespace eFinanzer.Common.Tasks.GetRoleChanges.Errors;

public record LoadCurrentUserRolesError(Error ParentError) : Error($"Failed to get role changes. Could not load the current user roles", ParentError);