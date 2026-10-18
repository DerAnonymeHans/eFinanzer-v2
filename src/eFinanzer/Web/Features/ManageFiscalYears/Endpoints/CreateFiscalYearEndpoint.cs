using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageFiscalYears.Endpoints;

public class CreateFiscalYearEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/manage-fiscal-years/create-fiscal-year", Handle)
            .WithName("CreateFiscalYear")
            .WithTags("ManageFiscalYears");

    private static async Task<Results<
        Created,
        BadRequest<InvalidInputError>,
        NotFound<NotFoundError>,
        Conflict<FiscalYearAlreadyExistsError>,
        Conflict<FiscalYearOverlapsExistingError>>> Handle(
        CreateFiscalYearCommand command,
        ICreateFiscalYearTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .ForAccountingUnit(command.AccountingUnitKey)
            .MustBeOneOf([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator], [AccountingUnitRole.FinanceOfficer])
            .Validate();

        var result = await task.Create(command, ct);
        if (result.IsSuccess)
            return TypedResults.Created();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            FiscalYearAlreadyExistsError error => TypedResults.Conflict(error),
            FiscalYearOverlapsExistingError error => TypedResults.Conflict(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
