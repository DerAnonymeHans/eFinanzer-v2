using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageAdministrationRoles.Endpoints;

public class GetAdministrationUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-administration-roles/get-administration-users", Handle)
            .WithName("GetAdministrationUsers")
            .WithTags("ManageAdministrationRoles");

    private static async Task<Results<
        Ok<GetAdministrationUsersDto>,
        InternalServerError<LoadAdministrationRoleGroupsError>,
        InternalServerError<LoadAdministrationRoleMembersError>>> Handle(
        IGetAdministrationUsersTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAdministrationRolesGroups())
            .Validate();

        var result = await task.GetUsers(ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error switch
        {
            LoadAdministrationRoleGroupsError error => TypedResults.InternalServerError(error),
            LoadAdministrationRoleMembersError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
