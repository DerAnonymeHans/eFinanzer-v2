using System.Diagnostics;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.GetUsers.Tasks.GetUser;
using eFinanzer.Features.GetUsers.Tasks.GetUser.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.GetUsers.Endpoints;

public class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/get-users/get-user", Handle)
            .WithName("GetUser")
            .WithTags("GetUsers");

    private static async Task<Results<
        Ok<GetUserDto>,
        NotFound<NotFoundError>>> Handle(
        [FromQuery] string username,
        IGetUserTask task,
        CancellationToken ct)
    {
        var result = await task.Get(username, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);


        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
