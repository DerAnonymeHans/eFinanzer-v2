using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class UpdateAccountingUnitEndpoint : IEndpoint
{
    /// <summary>
    /// The key is taken from the query, so the body only contains the changeable fields
    /// </summary>
    public record Request
    {
        public required string? Name { get; init; }
        public required string? Description { get; init; }

        /// <summary>
        /// Complete list of users and their roles. Users that are not listed lose all their roles in the accounting
        /// unit, so an empty list removes all users.
        /// </summary>
        public required List<UpdateAccountingUnitCommand.UserRoles> UsersWithRoles { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-accounting-units/update-accounting-unit", Handle)
            .WithName("UpdateAccountingUnit")
            .WithTags("ManageAccountingUnits");

    private static async Task<Results<
        NoContent,
        BadRequest<InvalidInputError>,
        BadRequest<UnknownUserError>,
        NotFound<NotFoundError>,
        InternalServerError<LoadCurrentUserRolesError>,
        InternalServerError<ChangeUserRoleError>>> Handle(
        [FromQuery] string key,
        Request request,
        IUpdateAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator)
            .Validate();

        var command = new UpdateAccountingUnitCommand
        {
            Key = key,
            Name = request.Name,
            Description = request.Description,
            UsersWithRoles = request.UsersWithRoles,
        };

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            UnknownUserError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            LoadCurrentUserRolesError error => TypedResults.InternalServerError(error),
            ChangeUserRoleError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
