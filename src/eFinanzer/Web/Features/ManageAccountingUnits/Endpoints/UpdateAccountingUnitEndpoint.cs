using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
// Results<> takes at most six results, so the server errors are nested
using InternalServerErrors = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.InternalServerError<eFinanzer.Common.Tasks.GetRoleChanges.Errors.LoadCurrentUserRolesError>,
    Microsoft.AspNetCore.Http.HttpResults.InternalServerError<eFinanzer.Common.Tasks.GetRoleChanges.Errors.ChangeUserRoleError>,
    Microsoft.AspNetCore.Http.HttpResults.InternalServerError<eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors.SaveAccountingUnitLogoError>>;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class UpdateAccountingUnitEndpoint : IEndpoint
{
    /// <summary>
    /// The key is taken from the query, so the body only contains the changeable fields. Sent as multipart/form-data,
    /// so the logo can be uploaded together with the other fields.
    /// </summary>
    public record UpdateAccountingUnitRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Complete list of users and their roles. Users that are not listed lose all their roles in the accounting
        /// unit, so an empty list removes all users.
        /// </summary>
        public List<UpdateAccountingUnitCommand.UserRoles>? UsersWithRoles { get; init; }

        /// <summary>
        /// Replaces the current logo. Without one, the current logo is kept.
        /// </summary>
        public IFormFile? Logo { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-accounting-units/update-accounting-unit", Handle)
            .WithName("UpdateAccountingUnit")
            .WithTags("ManageAccountingUnits")
            // Only the bearer token authenticates, so forms cannot be forged cross site
            .DisableAntiforgery();

    private static async Task<Results<
        NoContent,
        BadRequest<InvalidInputError>,
        BadRequest<UnknownUserError>,
        NotFound<NotFoundError>,
        InternalServerErrors>> Handle(
        [FromQuery] string key,
        [FromForm] UpdateAccountingUnitRequest request,
        IUpdateAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAccountingUnitGroups())
            .Validate();

        var command = new UpdateAccountingUnitCommand
        {
            Key = key,
            Name = request.Name,
            Description = request.Description,
            UsersWithRoles = request.UsersWithRoles ?? [],
            Logo = request.Logo,
        };

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            UnknownUserError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            LoadCurrentUserRolesError error => (InternalServerErrors)TypedResults.InternalServerError(error),
            ChangeUserRoleError error => (InternalServerErrors)TypedResults.InternalServerError(error),
            SaveAccountingUnitLogoError error => (InternalServerErrors)TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
