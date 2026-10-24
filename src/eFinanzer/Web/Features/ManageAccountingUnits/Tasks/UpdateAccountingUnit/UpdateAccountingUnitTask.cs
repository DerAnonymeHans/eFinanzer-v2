using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges;
using eFinanzer.Common.Tasks.GetRoleChanges.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;

public interface IUpdateAccountingUnitTask
{
    Task<Result> Update(UpdateAccountingUnitCommand command, CancellationToken ct);
}

public class UpdateAccountingUnitTask(
    EFinanzerDbContext context,
    IGetAccountingUnitTask getAccountingUnitTask,
    IHandleRoleChangesTask handleRoleChangesTask,
    ILogger<UpdateAccountingUnitTask> logger
    ) : IUpdateAccountingUnitTask
{

    public async Task<Result> Update(UpdateAccountingUnitCommand command, CancellationToken ct)
    {
        Result[] validations =
        [
            AccountingUnitValidation.ValidateName(command.Name),
            AccountingUnitValidation.ValidateDescription(command.Description),
            ValidateUsersWithRoles(command.UsersWithRoles),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        // Everything that can fail is resolved before anything is changed
        var roleChangesResult = await GetRoleChanges(command.Key, command.UsersWithRoles, ct);
        if (roleChangesResult.IsError)
            return roleChangesResult.Error;

        var updatedRows = await context.AccountingUnits
            .Where(x => x.Key == command.Key)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, command.Name)
                .SetProperty(x => x.Description, command.Description), ct);

        if (updatedRows == 0)
            return new NotFoundError(nameof(AccountingUnit), command.Key);

        logger.LogInformation("Updated accounting unit {AccountingUnitKey}", command.Key);

        return await handleRoleChangesTask.ExecuteRoleChanges(roleChangesResult.Value, ct);
    }

    /// <summary>
    /// Diffs the requested roles with the current ones in Keycloak and resolves the user and group ids needed to apply
    /// them
    /// </summary>
    private async Task<Result<RoleChange<AccountingUnitRole>[]>> GetRoleChanges(string key,
        List<UpdateAccountingUnitCommand.UserRoles> usersWithRoles, CancellationToken ct)
    {
        var currentResult = await getAccountingUnitTask.Get(key, ct);
        if (currentResult.IsError)
        {
            if (currentResult.Error is NotFoundError)
                return currentResult.Error;

            logger.LogError("Loading the current user roles of accounting unit {AccountingUnitKey} failed: {Error}",
                key, currentResult.Error);
            return new LoadCurrentUserRolesError(currentResult.Error);
        }

        return await handleRoleChangesTask.GetRoleChanges(
            currentResult.Value.UsersWithRoles.Select(x => new UserWithRoles<AccountingUnitRole>(x.UserName, x.Roles)),
            usersWithRoles.Select(x => new UserWithRoles<AccountingUnitRole>(x.UserName, x.Roles)),
            role => KeycloakGroups.GetAccountingUnitRoleGroup(key, role),
            ct
        );
    }

    /// <summary>
    /// The JSON deserialization does not enforce non-nullable references and accepts undefined enum values, so they are
    /// checked here
    /// </summary>
    private static Result ValidateUsersWithRoles(List<UpdateAccountingUnitCommand.UserRoles>? usersWithRoles)
    {
        const string field = nameof(UpdateAccountingUnitCommand.UsersWithRoles);

        if (usersWithRoles is null)
            return new InvalidInputError(field, "UsersWithRoles must not be null, use an empty list to remove all users");

        if (usersWithRoles.Any(x => string.IsNullOrWhiteSpace(x.UserName)))
            return new InvalidInputError(field, "Usernames must not be empty");

        var undefinedRoles = usersWithRoles.SelectMany(x => x.Roles).Where(x => !Enum.IsDefined(x)).ToList();
        if (undefinedRoles.Count > 0)
            return new InvalidInputError(field, $"Role '{undefinedRoles[0]}' does not exist");

        var duplicate = usersWithRoles
            .GroupBy(x => x.UserName.ToLowerInvariant())
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            return new InvalidInputError(field, $"User '{duplicate.Key}' is listed more than once");

        return Result.Ok();
    }
}
