using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears;

public interface IGetFiscalYearTask
{
    Task<Result<List<GetFiscalYearManagementListDto>>> GetAll(string accountingUnitKey, CancellationToken ct);
}

public class GetFiscalYearsTask(
    EFinanzerDbContext context
    ) : IGetFiscalYearTask
{
    public async Task<Result<List<GetFiscalYearManagementListDto>>> GetAll(string accountingUnitKey, CancellationToken ct)
    {
        // Distinguishes an unknown accounting unit from one without fiscal years
        var accountingUnitExists = await context.AccountingUnits.AnyAsync(x => x.Key == accountingUnitKey, ct);
        if (!accountingUnitExists)
            return new NotFoundError(nameof(AccountingUnit), accountingUnitKey);

        var fiscalYears = await context.FiscalYears
            .Where(x => x.AccountingUnitKey == accountingUnitKey)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new GetFiscalYearManagementListDto
            {
                Key = x.Key,
                AccountingUnitKey = x.AccountingUnitKey,
                Name = x.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                ReceiptsCount = x.Receipts!.Count(),
                BudgetsCount = x.Budgets!.Count(),
            })
            .ToListAsync(ct);

        return fiscalYears;
    }
}
