using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
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
        InternalServerError<ChangeAdministrationRoleError>>> Handle(
        UpdateAdministrationRolesCommand command,
        IUpdateAdministrationRolesTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAdministrationRolesGroups())
            .Validate();

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            UnknownUserError error => TypedResults.BadRequest(error),
            LoadCurrentAdministrationRolesError error => TypedResults.InternalServerError(error),
            ChangeAdministrationRoleError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
