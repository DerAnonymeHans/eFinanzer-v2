using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageFiscalYears.Endpoints;

public class UpdateFiscalYearEndpoint : IEndpoint
{
    /// <summary>
    /// The keys are taken from the query, so the body only contains the changeable fields
    /// </summary>
    public record Request
    {
        public required string? Name { get; init; }
        public required DateOnly StartDate { get; init; }
        public required DateOnly EndDate { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/manage-fiscal-years/update-fiscal-year", Handle)
            .WithName("UpdateFiscalYear")
            .WithTags("ManageFiscalYears");

    private static async Task<Results<
        NoContent,
        BadRequest<InvalidInputError>,
        NotFound<NotFoundError>,
        Conflict<FiscalYearOverlapsExistingError>,
        Conflict<FiscalYearWithBookingsShortenedError>>> Handle(
        [FromQuery] string accountingUnitKey,
        [FromQuery] string key,
        Request request,
        IUpdateFiscalYearTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .ForAccountingUnit(accountingUnitKey)
            .MustBeOneOf(GroupRules.GetManageFiscalYearGroups(accountingUnitKey))
            .Validate();

        var command = new UpdateFiscalYearCommand
        {
            Key = key,
            AccountingUnitKey = accountingUnitKey,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
        };

        var result = await task.Update(command, ct);
        if (result.IsSuccess)
            return TypedResults.NoContent();

        return result.Error switch
        {
            InvalidInputError error => TypedResults.BadRequest(error),
            NotFoundError error => TypedResults.NotFound(error),
            FiscalYearOverlapsExistingError error => TypedResults.Conflict(error),
            FiscalYearWithBookingsShortenedError error => TypedResults.Conflict(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
