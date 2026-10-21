using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class GetOwnRoleChangeRequestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-role-change-requests/list-own-role-change-requests", Handle)
            .WithName("GetOwnRoleChangeRequests")
            .WithTags("ManageRoleChangeRequests");

    /// <summary>
    /// No group check: every signed-in user may see their own requests
    /// </summary>
    private static async Task<Ok<GetRoleChangeRequestDto[]>> Handle(
        IGetRoleChangeRequestsTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var result = await task.GetOwn(user.GetUsername(), ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        throw new UnreachableException($"Unhandled error: {result.Error}");
    }
}
