using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class DeleteAccountingUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("/manage-accounting-units/delete-accounting-unit", Handle)
            .WithName("DeleteAccountingUnit")
            .WithTags("ManageAccountingUnits");

    private static async Task<Results<
        NoContent,
        NotFound<NotFoundError>,
        Conflict<AccountingUnitHasFiscalYearsError>,
        InternalServerError<DeleteAccountingUnitGroupError>>> Handle(
        [FromQuery] string key,
        IDeleteAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.ForAdministration()
            .MustBeOneOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);
        
        var result = await task.Delete(key, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            AccountingUnitHasFiscalYearsError error => TypedResults.Conflict(error),
            DeleteAccountingUnitGroupError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
