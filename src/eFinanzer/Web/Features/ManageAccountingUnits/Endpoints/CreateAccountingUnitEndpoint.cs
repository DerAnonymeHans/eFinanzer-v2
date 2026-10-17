using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class CreateAccountingUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/manage-accounting-units/create-accounting-unit", Handle)
            .WithName("CreateAccountingUnit")
            .WithTags("ManageAccountingUnits");

    private static async Task<Results<Created, BadRequest<InvalidInputError>>> Handle(
        CreateAccountingUnitCommand command,
        ICreateAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.ForAdministration()
            .MustBeOneOf(AdministrationRoles.FinanceAdministrator, AdministrationRoles.HeadFinanceOfficer);
        
        var result = await task.Create(command, ct);
        if (result.IsSuccess)
            return TypedResults.Created($"/api/accounting-units/{command.Key}");

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
