using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageFiscalYears.Endpoints;

public class DeleteFiscalYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("/manage-fiscal-years/delete-fiscal-year", Handle)
            .WithName("DeleteFiscalYear")
            .WithTags("ManageFiscalYears");

    private static async Task<Results<
        NoContent,
        NotFound<NotFoundError>,
        Conflict<FiscalYearHasBookingsError>>> Handle(
        [FromQuery] string accountingUnitKey,
        [FromQuery] string key,
        IDeleteFiscalYearTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .ForAccountingUnit(accountingUnitKey)
            .MustBeOneOf([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator], [AccountingUnitRole.FinanceOfficer])
            .Validate();

        var result = await task.Delete(accountingUnitKey, key, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            FiscalYearHasBookingsError error => TypedResults.Conflict(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
