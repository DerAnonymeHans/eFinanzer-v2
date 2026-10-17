using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;

public interface IUpdateAccountingUnitTask
{
    Task<Result> Update(UpdateAccountingUnitCommand command, CancellationToken ct);
}

public class UpdateAccountingUnitTask(
    EFinanzerDbContext context,
    ILogger<UpdateAccountingUnitTask> logger
    ) : IUpdateAccountingUnitTask
{
    public async Task<Result> Update(UpdateAccountingUnitCommand command, CancellationToken ct)
    {
        // The key is the primary key and referenced by fiscal years, so only name and description can change
        var updatedRows = await context.AccountingUnits
            .Where(x => x.Key == command.Key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, command.Name)
                .SetProperty(x => x.Description, command.Description), ct);

        if (updatedRows == 0)
            return new NotFoundError(nameof(AccountingUnit), command.Key);

        logger.LogInformation("Updated accounting unit {AccountingUnitKey}", command.Key);
        return Result.Ok();
    }
}
