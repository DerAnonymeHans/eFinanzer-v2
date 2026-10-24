using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Models;

public record UpdateAdministrationRolesCommand
{
    public required string Username { get; init; }

    /// <summary>
    /// Complete list of the administration roles of the user. Roles that are not listed are removed, so an empty list
    /// removes all administration roles of the user.
    /// </summary>
    public required AdministrationRole[] AdministrationRoles { get; init; }
}
