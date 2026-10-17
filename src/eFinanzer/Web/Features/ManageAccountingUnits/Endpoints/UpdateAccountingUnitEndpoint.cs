using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class UpdateAccountingUnitEndpoint : IEndpoint
{
    /// <summary>
    /// The key is taken from the route, so the body only contains the changeable fields
    /// </summary>
    public record Request
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-accounting-units/update-accounting-unit", Handle)
            .WithName("UpdateAccountingUnit")
            .WithTags("ManageAccountingUnits");

    private static async Task<Results<NoContent, NotFound<NotFoundError>>> Handle(
        [FromQuery] string key,
        Request request,
        IUpdateAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.ForAdministration()
            .MustBeOneOf(AdministrationRoles.HeadFinanceOfficer, AdministrationRoles.FinanceAdministrator);
        
        var command = new UpdateAccountingUnitCommand
        {
            Key = key,
            Name = request.Name,
            Description = request.Description,
        };

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
