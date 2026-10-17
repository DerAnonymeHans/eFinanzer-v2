using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit.Errors;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit;

public interface IDeleteAccountingUnitTask
{
    Task<Result> Delete(string key, CancellationToken ct);
}

public class DeleteAccountingUnitTask(
    EFinanzerDbContext context,
    ILogger<DeleteAccountingUnitTask> logger
    ) : IDeleteAccountingUnitTask
{
    public async Task<Result> Delete(string key, CancellationToken ct)
    {
        var exists = await context.AccountingUnits.AnyAsync(x => x.Key == key, ct);
        if (!exists)
            return new NotFoundError(nameof(AccountingUnit), key);

        // Fiscal years hold the bookings, which must not be lost by deleting their accounting unit
        var hasFiscalYears = await context.FiscalYears.AnyAsync(x => x.AccountingUnitKey == key, ct);
        if (hasFiscalYears)
            return new AccountingUnitHasFiscalYearsError(key);

        await context.AccountingUnits
            .Where(x => x.Key == key)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Deleted accounting unit {AccountingUnitKey}", key);
        return Result.Ok();
    }
}
