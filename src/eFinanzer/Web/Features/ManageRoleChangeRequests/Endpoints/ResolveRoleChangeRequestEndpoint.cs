using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class ResolveRoleChangeRequestEndpoint : IEndpoint
{
    /// <summary>
    /// The id is taken from the query and the resolving user from the token, so the body only contains the decision
    /// </summary>
    public record ResolveRoleChangeRequestRequest
    {
        public required RoleChangeRequest.ResolutionStatus Resolution { get; init; }
        public string? Comment { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-role-change-requests/resolve-role-change-request", Handle)
            .WithName("ResolveRoleChangeRequest")
            .WithTags("ManageRoleChangeRequests");

    private static async Task<Results<
        NoContent,
        BadRequest<InvalidInputError>,
        NotFound<NotFoundError>,
        Conflict<RoleChangeRequestAlreadyResolvedError>,
        Conflict<RoleChangeRequestNotApplicableError>,
        InternalServerError<ApplyRoleChangeError>>> Handle(
        [FromQuery] int id,
        ResolveRoleChangeRequestRequest request,
        IResolveRoleChangeRequestTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetResolveRoleChangeRequestsGroups())
            .Validate();

        var command = new ResolveRoleChangeRequestCommand
        {
            Id = id,
            ResolvedBy = user.GetUsername(),
            Resolution = request.Resolution,
            Comment = request.Comment,
        };

        var result = await task.Resolve(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            RoleChangeRequestAlreadyResolvedError error => TypedResults.Conflict(error),
            RoleChangeRequestNotApplicableError error => TypedResults.Conflict(error),
            ApplyRoleChangeError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
