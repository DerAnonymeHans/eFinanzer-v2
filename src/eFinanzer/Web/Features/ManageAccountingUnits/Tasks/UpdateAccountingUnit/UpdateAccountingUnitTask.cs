using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;
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
    IKeycloakUserClient keycloakUserClient,
    ILogger<UpdateAccountingUnitTask> logger
    ) : IUpdateAccountingUnitTask
{
    private record UserRole(string UserName, AccountingUnitRole Role);

    private record RoleChange(string UserName, string UserId, AccountingUnitRole Role, string GroupId, bool Added);

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

        foreach (var change in roleChangesResult.Value)
        {
            var changeResult = change.Added
                ? await keycloakUserClient.AddUserToGroup(change.UserId, change.GroupId, ct)
                : await keycloakUserClient.RemoveUserFromGroup(change.UserId, change.GroupId, ct);

            if (changeResult.IsError)
            {
                logger.LogError(
                    "Changing role {Role} (added: {Added}) of user {UserName} in accounting unit {AccountingUnitKey} failed: {Error}",
                    change.Role, change.Added, change.UserName, command.Key, changeResult.Error);
                return new ChangeUserRoleError(command.Key, change.UserName, change.Role, change.Added, changeResult.Error);
            }

            logger.LogInformation(
                "Changed role {Role} (added: {Added}) of user {UserName} in accounting unit {AccountingUnitKey}",
                change.Role, change.Added, change.UserName, command.Key);
        }

        return Result.Ok();
    }

    /// <summary>
    /// Diffs the requested roles with the current ones in Keycloak and resolves the user and group ids needed to apply
    /// them
    /// </summary>
    private async Task<Result<List<RoleChange>>> GetRoleChanges(string key,
        List<UpdateAccountingUnitCommand.UserRoles> usersWithRoles, CancellationToken ct)
    {
        var currentResult = await getAccountingUnitTask.Get(key, ct);
        if (currentResult.IsError)
        {
            if (currentResult.Error is NotFoundError)
                return currentResult.Error;

            logger.LogError("Loading the current user roles of accounting unit {AccountingUnitKey} failed: {Error}",
                key, currentResult.Error);
            return new LoadCurrentUserRolesError(key, currentResult.Error);
        }

        // Keycloak stores usernames in lower case
        var current = currentResult.Value.UsersWithRoles
            .SelectMany(user => user.Roles.Select(role => new UserRole(user.UserName.ToLowerInvariant(), role)))
            .ToHashSet();
        var requested = usersWithRoles
            .SelectMany(user => user.Roles.Select(role => new UserRole(user.UserName.ToLowerInvariant(), role)))
            .ToHashSet();

        var added = requested.Except(current).ToList();
        var removed = current.Except(requested).ToList();

        var userIds = new Dictionary<string, string>();
        foreach (var userName in added.Concat(removed).Select(x => x.UserName).Distinct())
        {
            var userResult = await keycloakUserClient.GetUserByUsername(userName, ct);
            if (userResult.IsError)
                return new UnknownUserError(userName);

            userIds[userName] = userResult.Value.Id;
        }

        var groupIds = new Dictionary<AccountingUnitRole, string>();
        foreach (var role in added.Concat(removed).Select(x => x.Role).Distinct())
        {
            var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(
                KeycloakGroups.GetAccountingUnitRoleGroup(key, role), ct);
            if (groupIdResult.IsError)
            {
                logger.LogError("Loading the group of role {Role} of accounting unit {AccountingUnitKey} failed: {Error}",
                    role, key, groupIdResult.Error);
                return new LoadCurrentUserRolesError(key, groupIdResult.Error);
            }

            groupIds[role] = groupIdResult.Value;
        }

        // Removals first, so a failure leaves users with fewer instead of more rights
        return removed.Select(x => new RoleChange(x.UserName, userIds[x.UserName], x.Role, groupIds[x.Role], Added: false))
            .Concat(added.Select(x => new RoleChange(x.UserName, userIds[x.UserName], x.Role, groupIds[x.Role], Added: true)))
            .ToList();
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
