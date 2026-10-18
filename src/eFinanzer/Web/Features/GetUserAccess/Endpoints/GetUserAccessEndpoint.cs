using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Endpoints;
using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess;
using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.GetUserAccess.Endpoints;

public class GetUserAccessEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/get-user-access/my-user-access", Handle)
            .WithName("GetUserAccess")
            .WithTags("GetUserAccess");

    /// <summary>
    /// No group check: every signed-in user may see their own access
    /// </summary>
    private static async Task<Ok<UserAccess>> Handle(
        IGetUserAccessTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var result = await task.Get(user, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        throw new UnreachableException($"Unhandled error: {result.Error}");
    }
}
