using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class CreateRoleChangeRequestEndpoint : IEndpoint
{
    /// <summary>
    /// The requesting user is taken from the token, so the body only contains the request itself
    /// </summary>
    public record CreateRoleChangeRequestBody
    {
        public string? Comment { get; init; }
        public required CreateRoleChangeRequestRequest.RoleChange[] RoleChanges { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/manage-role-change-requests/create-role-change-request", Handle)
            .WithName("CreateRoleChangeRequest")
            .WithTags("ManageRoleChangeRequests");

    /// <summary>
    /// No group check: every signed-in user may request roles for themselves
    /// </summary>
    private static async Task<Results<
        Created,
        BadRequest<InvalidInputError>,
        Conflict<PendingRoleChangeRequestAlreadyExistsError>,
        NotFound<NotFoundError>>> Handle(
        CreateRoleChangeRequestBody body,
        ICreateRoleChangeRequestTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var request = new CreateRoleChangeRequestRequest
        {
            RequestedBy = user.GetUsername(),
            Comment = body.Comment,
            RoleChanges = body.RoleChanges,
        };

        var result = await task.Create(request, ct);
        if (result.IsSuccess)
            return TypedResults.Created();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            PendingRoleChangeRequestAlreadyExistsError error => TypedResults.Conflict(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
