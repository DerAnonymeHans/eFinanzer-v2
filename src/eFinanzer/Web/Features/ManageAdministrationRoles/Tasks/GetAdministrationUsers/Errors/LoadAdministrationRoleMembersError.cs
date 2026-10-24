namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Errors;

public record LoadAdministrationRoleMembersError(string GroupPath, Error ParentError)
    : Error($"Could not load the members of administration group '{GroupPath}' from Keycloak", ParentError);
