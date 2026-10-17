using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;

public interface ICreateAccountingUnitTask
{
    Task<Result> Create(CreateAccountingUnitCommand command, CancellationToken ct);
}

public class CreateAccountingUnitTask(
    EFinanzerDbContext context,
    IKeycloakUserClient keycloakUserClient,
    ILogger<CreateAccountingUnitTask> logger
    ) : ICreateAccountingUnitTask
{
    public async Task<Result> Create(CreateAccountingUnitCommand command, CancellationToken ct)
    {
        Result[] validations =
        [
            AccountingUnitValidation.ValidateKey(command.Key),
            AccountingUnitValidation.ValidateName(command.Name),
            AccountingUnitValidation.ValidateDescription(command.Description),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        var keyExists = await context.AccountingUnits.AnyAsync(x => x.Key == command.Key, ct);
        if (keyExists)
            return new AccountingUnitKeyAlreadyExistsError(command.Key);

        // The unit is only committed once its groups exist, so there is no unit without groups
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        var accountingUnit = new AccountingUnit
        {
            Key = command.Key,
            Description = command.Description,
            Name = command.Name,
        };
        context.AccountingUnits.Add(accountingUnit);
        await context.SaveChangesAsync(ct);

        var createGroupsResult = await CreateGroups(command.Key, ct);
        if (createGroupsResult.IsError)
        {
            logger.LogError("Creating the Keycloak groups of accounting unit {AccountingUnitKey} failed: {Error}",
                command.Key, createGroupsResult.Error);
            return new CreateAccountingUnitGroupsError(command.Key, createGroupsResult.Error);
        }

        await transaction.CommitAsync(ct);

        logger.LogInformation("Created accounting unit {AccountingUnitKey}", command.Key);
        return Result.Ok();
    }

    /// <summary>
    /// Creates the group of the accounting unit with one subgroup per <see cref="AccountingUnitRole"/>. Groups that
    /// already exist, e.g. from an earlier failed attempt, are kept.
    /// </summary>
    private async Task<Result> CreateGroups(string key, CancellationToken ct)
    {
        var parentResult = await keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.AccountingUnitsGroup, ct);
        if (parentResult.IsError)
            return parentResult.Error;

        var unitGroupResult = await GetOrCreateSubgroup(parentResult.Value, KeycloakGroups.GetAccountingUnitGroup(key), key, ct);
        if (unitGroupResult.IsError)
            return unitGroupResult.Error;

        foreach (var role in Enum.GetValues<AccountingUnitRole>())
        {
            var roleGroupResult = await GetOrCreateSubgroup(unitGroupResult.Value,
                KeycloakGroups.GetAccountingUnitRoleGroup(key, role), KeycloakGroups.GetGroupName(role), ct);
            if (roleGroupResult.IsError)
                return roleGroupResult.Error;
        }

        return Result.Ok();
    }

    private async Task<Result<string>> GetOrCreateSubgroup(string parentGroupId, string groupPath, string name,
        CancellationToken ct)
    {
        var createResult = await keycloakUserClient.CreateSubgroup(parentGroupId, name, ct);
        if (createResult.Error is not AlreadyExistsError)
            return createResult;

        logger.LogWarning("Keycloak group {GroupPath} already exists, keeping it including its members", groupPath);
        return await keycloakUserClient.GetGroupIdFromPath(groupPath, ct);
    }
}
