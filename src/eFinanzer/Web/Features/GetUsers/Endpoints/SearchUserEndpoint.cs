using System.Diagnostics;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.GetUsers.Tasks.GetUser;
using eFinanzer.Features.GetUsers.Tasks.GetUser.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.GetUsers.Endpoints;

public class SearchUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/get-users/search-user", Handle)
            .WithName("SearchUser")
            .WithTags("GetUsers");

    private static async Task<Results<
        Ok<List<GetUserDto>>,
        BadRequest<InvalidInputError>>> Handle(
        [FromQuery] string search,
        IGetUserTask task,
        CancellationToken ct)
    {
        var result = await task.Search(search, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
