using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;

public interface IGetAccountingUnitTask
{
    Task<Result<AccountingUnitModel>> Get(string key, CancellationToken ct);
    Task<Result<List<AccountingUnitModel>>> GetAll(CancellationToken ct);
}

public class GetAccountingUnitTask(
    EFinanzerDbContext context
    ) : IGetAccountingUnitTask
{
    public async Task<Result<AccountingUnitModel>> Get(string key, CancellationToken ct)
    {
        var accountingUnit = await context.AccountingUnits
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => new AccountingUnitModel
            {
                Key = x.Key,
                Name = x.Name,
                Description = x.Description,
            })
            .FirstOrDefaultAsync(ct);

        if (accountingUnit is null)
            return new NotFoundError(nameof(AccountingUnit), key);

        return accountingUnit;
    }

    public async Task<Result<List<AccountingUnitModel>>> GetAll(CancellationToken ct)
    {
        var accountingUnits = await context.AccountingUnits
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new AccountingUnitModel
            {
                Key = x.Key,
                Name = x.Name,
                Description = x.Description,
            })
            .ToListAsync(ct);

        return accountingUnits;
    }
}
