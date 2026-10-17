using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class GetAccountingUnitEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-accounting-units/get-accounting-unit", Handle)
            .WithName("GetAccountingUnit")
            .WithTags("ManageAccountingUnits");

    private static async Task<Results<
        Ok<GetAccountingUnitManagementDto>,
        NotFound<NotFoundError>,
        InternalServerError<LoadAccountingUnitRoleGroupsError>,
        InternalServerError<LoadAccountingUnitRoleMembersError>>> Handle(
        [FromQuery] string key,
        IGetAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.ForAdministration()
            .MustBeOneOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);

        var result = await task.Get(key, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            LoadAccountingUnitRoleGroupsError error => TypedResults.InternalServerError(error),
            LoadAccountingUnitRoleMembersError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}
