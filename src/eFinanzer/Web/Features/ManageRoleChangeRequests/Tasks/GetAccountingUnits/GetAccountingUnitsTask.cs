using eFinanzer.Common.Database;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits;

public interface IGetAccountingUnitsTask
{
    /// <summary>
    /// All accounting units, ordered by key
    /// </summary>
    Task<Result<RoleChangeRequestAccountingUnitDto[]>> GetAll(CancellationToken ct);
}

public class GetAccountingUnitsTask(
    EFinanzerDbContext context
    ) : IGetAccountingUnitsTask
{
    public async Task<Result<RoleChangeRequestAccountingUnitDto[]>> GetAll(CancellationToken ct)
    {
        var accountingUnits = await context.AccountingUnits
            .OrderBy(x => x.Key)
            .Select(x => new RoleChangeRequestAccountingUnitDto
            {
                Key = x.Key,
                Name = x.Name,
            })
            .ToArrayAsync(ct);

        return accountingUnits;
    }
}
