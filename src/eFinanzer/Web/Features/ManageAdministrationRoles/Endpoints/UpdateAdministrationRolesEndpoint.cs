using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageAdministrationRoles.Endpoints;

public class UpdateAdministrationRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-administration-roles/update-administration-roles", Handle)
            .WithName("UpdateAdministrationRoles")
            .WithTags("ManageAdministrationRoles");

    private static async Task<Results<
        NoContent,
        BadRequest<InvalidInputError>,
        BadRequest<UnknownUserError>,
        InternalServerError<LoadCurrentAdministrationRolesError>,
        InternalServerError<LoadCurrentUserRolesError>,
        InternalServerError<ChangeUserRoleError>>> Handle(
        UpdateAdministrationRolesCommand command,
        IUpdateAdministrationRolesTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAdministrationRolesGroups())
            .Validate();

        // Without a managing role the user could not undo the change, and the last one would lock everyone out
        var isOwnUser = string.Equals(command.Username, user.GetUsername(), StringComparison.OrdinalIgnoreCase);
        var keepsManagingRole = command.AdministrationRoles?
            .Select(KeycloakGroups.GetAdministrationRoleGroup)
            .Any(GroupRules.GetManageAdministrationRolesGroups().Contains) ?? false;
        if (isOwnUser && !keepsManagingRole)
            return TypedResults.BadRequest(new InvalidInputError(nameof(UpdateAdministrationRolesCommand.AdministrationRoles),
                "You cannot remove your own role to manage administration roles"));

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            UnknownUserError error => TypedResults.BadRequest(error),
            LoadCurrentAdministrationRolesError error => TypedResults.InternalServerError(error),
            LoadCurrentUserRolesError error => TypedResults.InternalServerError(error),
            ChangeUserRoleError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
