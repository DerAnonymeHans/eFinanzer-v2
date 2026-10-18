using System.Diagnostics;
using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

public class GetAccountingUnitsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-accounting-units/list-accounting-units", Handle)
            .WithName("GetAccountingUnits")
            .WithTags("ManageAccountingUnits");

    private static async Task<Ok<List<GetAccountingUnitManagementListDto>>> Handle(
        IGetAccountingUnitTask task,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        user.CreateClaimBuilder()
            .MustBeOneOf(GroupRules.GetManageAccountingUnitGroups())
            .Validate();
        
        var result = await task.GetAll(ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        throw new UnreachableException($"Unhandled error: {result.Error}");
    }
}
