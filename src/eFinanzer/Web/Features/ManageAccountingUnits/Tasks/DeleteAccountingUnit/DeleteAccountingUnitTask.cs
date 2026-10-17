using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
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
    IKeycloakUserClient keycloakUserClient,
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

        // The unit is only deleted if its group is gone too, otherwise a new unit with the same key would get the
        // members of the old one
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        await context.AccountingUnits
            .Where(x => x.Key == key)
            .ExecuteDeleteAsync(ct);

        var deleteGroupResult = await DeleteGroup(key, ct);
        if (deleteGroupResult.IsError)
        {
            logger.LogError("Deleting the Keycloak group of accounting unit {AccountingUnitKey} failed: {Error}",
                key, deleteGroupResult.Error);
            return new DeleteAccountingUnitGroupError(key, deleteGroupResult.Error);
        }

        await transaction.CommitAsync(ct);

        logger.LogInformation("Deleted accounting unit {AccountingUnitKey}", key);
        return Result.Ok();
    }

    /// <summary>
    /// Deletes the group of the accounting unit including its role groups. A missing group counts as deleted.
    /// </summary>
    private async Task<Result> DeleteGroup(string key, CancellationToken ct)
    {
        var groupPath = KeycloakGroups.GetAccountingUnitGroup(key);
        var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(groupPath, ct);
        if (groupIdResult.Error is NotFoundError)
        {
            logger.LogWarning("Keycloak group {GroupPath} does not exist, nothing to delete", groupPath);
            return Result.Ok();
        }
        if (groupIdResult.IsError)
            return groupIdResult.Error;

        var deleteResult = await keycloakUserClient.DeleteGroup(groupIdResult.Value, ct);
        if (deleteResult.Error is NotFoundError)
            return Result.Ok();

        return deleteResult;
    }
}
