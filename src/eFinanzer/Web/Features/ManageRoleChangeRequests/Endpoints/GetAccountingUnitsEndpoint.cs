using System.Diagnostics;
using eFinanzer.Common.Endpoints;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace eFinanzer.Features.ManageRoleChangeRequests.Endpoints;

public class GetAccountingUnitsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-role-change-requests/list-accounting-units", Handle)
            .WithName("GetRoleChangeRequestAccountingUnits")
            .WithTags("ManageRoleChangeRequests");

    /// <summary>
    /// No group check: every signed-in user needs the accounting units to request roles in them
    /// </summary>
    private static async Task<Ok<RoleChangeRequestAccountingUnitDto[]>> Handle(
        IGetAccountingUnitsTask task,
        CancellationToken ct)
    {
        var result = await task.GetAll(ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value);

        throw new UnreachableException($"Unhandled error: {result.Error}");
    }
}
