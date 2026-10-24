namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Errors;

public record LoadAdministrationRoleGroupsError(Error ParentError)
    : Error("Could not load the role groups of the administration from Keycloak", ParentError);
