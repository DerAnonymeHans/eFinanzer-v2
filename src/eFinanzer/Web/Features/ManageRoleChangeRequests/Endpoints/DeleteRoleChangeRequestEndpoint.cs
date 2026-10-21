using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.DeleteRoleChangeRequest;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class DeleteRoleChangeRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("/manage-role-change-requests/delete-role-change-request", Handle)
            .WithName("DeleteRoleChangeRequest")
            .WithTags("ManageRoleChangeRequests");

    /// <summary>
    /// No group check: the task only deletes requests of the user themselves
    /// </summary>
    private static async Task<Results<
        NoContent,
        NotFound<NotFoundError>,
        Conflict<RoleChangeRequestAlreadyResolvedError>>> Handle(
        [FromQuery] int id,
        IDeleteRoleChangeRequestTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var result = await task.Delete(id, user.GetUsername(), ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            RoleChangeRequestAlreadyResolvedError error => TypedResults.Conflict(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
