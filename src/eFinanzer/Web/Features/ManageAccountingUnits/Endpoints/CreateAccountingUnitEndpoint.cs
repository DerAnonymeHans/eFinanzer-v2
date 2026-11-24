using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class CreateAccountingUnitEndpoint : IEndpoint
{
    /// <summary>
    /// Sent as multipart/form-data, so the logo can be uploaded together with the unit
    /// </summary>
    public record CreateAccountingUnitRequest
    {
        public required string Key { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Optional PNG, JPG or SVG logo of at most 5 MB
        /// </summary>
        public IFormFile? Logo { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/manage-accounting-units/create-accounting-unit", Handle)
            .WithName("CreateAccountingUnit")
            .WithTags("ManageAccountingUnits")
            // Only the bearer token authenticates, so forms cannot be forged cross site
            .DisableAntiforgery();

    private static async Task<Results<
        Created,
        BadRequest<InvalidInputError>,
        Conflict<AccountingUnitKeyAlreadyExistsError>,
        InternalServerError<CreateAccountingUnitGroupsError>,
        InternalServerError<SaveAccountingUnitLogoError>>> Handle(
        [FromForm] CreateAccountingUnitRequest request,
        ICreateAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAccountingUnitGroups())
            .Validate();
        
        var command = new CreateAccountingUnitCommand
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            Logo = request.Logo,
        };

        var result = await task.Create(command, ct);
        if (result.IsSuccess)
            return TypedResults.Created();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            AccountingUnitKeyAlreadyExistsError error => TypedResults.Conflict(error),
            CreateAccountingUnitGroupsError error => TypedResults.InternalServerError(error),
            SaveAccountingUnitLogoError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
