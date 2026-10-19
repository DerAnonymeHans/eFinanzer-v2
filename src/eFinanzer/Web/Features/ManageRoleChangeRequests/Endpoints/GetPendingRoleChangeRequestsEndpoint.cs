using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Pagination;
using eFinanzer.Common.Pagination.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class GetPendingRoleChangeRequestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-role-change-requests/list-pending-role-change-requests", Handle)
            .WithName("GetPendingRoleChangeRequests")
            .WithTags("ManageRoleChangeRequests");

    private static async Task<Results<
        Ok<Page<GetRoleChangeRequestDto>>, 
        BadRequest<InvalidOffsetPaginationError>>> Handle(
        [AsParameters] OffsetPagination pagination,
        IGetRoleChangeRequestsTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetResolveRoleChangeRequestsGroups())
            .Validate();

        var result = await task.GetPending(pagination, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error switch
        {
            InvalidOffsetPaginationError error => TypedResults.BadRequest(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
