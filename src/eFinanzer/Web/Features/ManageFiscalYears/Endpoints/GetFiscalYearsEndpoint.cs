using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageFiscalYears.Endpoints;

public class GetFiscalYearsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-fiscal-years/list-fiscal-years", Handle)
            .WithName("GetFiscalYears")
            .WithTags("ManageFiscalYears");

    private static async Task<Results<
        Ok<List<GetFiscalYearManagementListDto>>,
        NotFound<NotFoundError>>> Handle(
        [FromQuery] string accountingUnitKey,
        IGetFiscalYearTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .ForAccountingUnit(accountingUnitKey)
            .MustBeOneOf([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator], [AccountingUnitRole.FinanceOfficer])
            .Validate();

        var result = await task.GetAll(accountingUnitKey, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
