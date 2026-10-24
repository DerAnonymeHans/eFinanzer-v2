namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;

public record LoadCurrentAdministrationRolesError(Error ParentError)
    : Error("Could not load the current administration roles", ParentError);
