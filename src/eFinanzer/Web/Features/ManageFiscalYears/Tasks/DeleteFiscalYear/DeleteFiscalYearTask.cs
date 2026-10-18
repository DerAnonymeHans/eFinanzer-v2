using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear.Errors;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear;

public interface IDeleteFiscalYearTask
{
    Task<Result> Delete(string accountingUnitKey, string key, CancellationToken ct);
}

public class DeleteFiscalYearTask(
    EFinanzerDbContext context,
    ILogger<DeleteFiscalYearTask> logger
    ) : IDeleteFiscalYearTask
{
    public async Task<Result> Delete(string accountingUnitKey, string key, CancellationToken ct)
    {
        var fiscalYear = await context.FiscalYears
            .Where(x => x.Key == key && x.AccountingUnitKey == accountingUnitKey)
            .Select(x => new { HasBookings = x.Budgets!.Any() || x.Receipts!.Any() })
            .FirstOrDefaultAsync(ct);

        if (fiscalYear is null)
            return new NotFoundError(nameof(FiscalYear), $"{accountingUnitKey}/{key}");

        // Budgets and receipts must not be lost by deleting their fiscal year
        if (fiscalYear.HasBookings)
            return new FiscalYearHasBookingsError(key, accountingUnitKey);

        await context.FiscalYears
            .Where(x => x.Key == key && x.AccountingUnitKey == accountingUnitKey)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Deleted fiscal year {FiscalYearKey} of accounting unit {AccountingUnitKey}",
            key, accountingUnitKey);
        return Result.Ok();
    }
}
