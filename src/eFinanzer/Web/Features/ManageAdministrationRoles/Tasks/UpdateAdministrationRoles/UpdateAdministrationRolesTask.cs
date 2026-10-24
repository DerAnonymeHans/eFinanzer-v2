using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles;

public interface IUpdateAdministrationRolesTask
{
    Task<Result> Update(UpdateAdministrationRolesCommand command, CancellationToken ct);
}

public class UpdateAdministrationRolesTask(
    IKeycloakUserClient keycloakUserClient,
    IGetAdministrationUsersTask getAdministrationUsersTask,
    ILogger<UpdateAdministrationRolesTask> logger
    ) : IUpdateAdministrationRolesTask
{
    private record RoleChange(string UserName, string UserId, AdministrationRole Role, string GroupId, bool Added);

    public async Task<Result> Update(UpdateAdministrationRolesCommand command, CancellationToken ct)
    {
        var validation = Validate(command);
        if (validation.IsError)
            return validation;

        // Everything that can fail is resolved before anything is changed
        var roleChangesResult = await GetRoleChanges(command, ct);
        if (roleChangesResult.IsError)
            return roleChangesResult.Error;

        foreach (var change in roleChangesResult.Value)
        {
            var changeResult = change.Added
                ? await keycloakUserClient.AddUserToGroup(change.UserId, change.GroupId, ct)
                : await keycloakUserClient.RemoveUserFromGroup(change.UserId, change.GroupId, ct);

            if (changeResult.IsError)
            {
                logger.LogError(
                    "Changing administration role {Role} (added: {Added}) of user {UserName} failed: {Error}",
                    change.Role, change.Added, change.UserName, changeResult.Error);
                return new ChangeAdministrationRoleError(change.UserName, change.Role, change.Added, changeResult.Error);
            }

            logger.LogInformation(
                "Changed administration role {Role} (added: {Added}) of user {UserName}",
                change.Role, change.Added, change.UserName);
        }

        return Result.Ok();
    }

    /// <summary>
    /// Diffs the requested roles with the current ones in Keycloak and resolves the user and group ids needed to apply
    /// them
    /// </summary>
    private async Task<Result<List<RoleChange>>> GetRoleChanges(UpdateAdministrationRolesCommand command,
        CancellationToken ct)
    {
        var currentResult = await getAdministrationUsersTask.GetUsers(ct);
        if (currentResult.IsError)
        {
            logger.LogError("Loading the current administration roles failed: {Error}", currentResult.Error);
            return new LoadCurrentAdministrationRolesError(currentResult.Error);
        }

        // Keycloak stores usernames in lower case
        var userName = command.Username.ToLowerInvariant();
        var current = currentResult.Value.AdministrationUsers
            .Where(x => x.Username.Equals(userName, StringComparison.InvariantCultureIgnoreCase))
            .SelectMany(x => x.Roles)
            .ToHashSet();
        var requested = command.AdministrationRoles.ToHashSet();

        var added = requested.Except(current).ToList();
        var removed = current.Except(requested).ToList();

        if (added.Count == 0 && removed.Count == 0)
            return new List<RoleChange>();

        var userResult = await keycloakUserClient.GetUserByUsername(userName, ct);
        if (userResult.IsError)
            return new UnknownUserError(userName);

        var userId = userResult.Value.Id;

        var groupIds = new Dictionary<AdministrationRole, string>();
        foreach (var role in added.Concat(removed))
        {
            var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(
                KeycloakGroups.GetAdministrationRoleGroup(role), ct);
            if (groupIdResult.IsError)
            {
                logger.LogError("Loading the group of administration role {Role} failed: {Error}",
                    role, groupIdResult.Error);
                return new LoadCurrentAdministrationRolesError(groupIdResult.Error);
            }

            groupIds[role] = groupIdResult.Value;
        }

        // Removals first, so a failure leaves the user with fewer instead of more rights
        return removed.Select(role => new RoleChange(userName, userId, role, groupIds[role], Added: false))
            .Concat(added.Select(role => new RoleChange(userName, userId, role, groupIds[role], Added: true)))
            .ToList();
    }

    /// <summary>
    /// The JSON deserialization does not enforce non-nullable references and accepts undefined enum values, so they are
    /// checked here
    /// </summary>
    private static Result Validate(UpdateAdministrationRolesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Username))
            return new InvalidInputError(nameof(UpdateAdministrationRolesCommand.Username), "Username must not be empty");

        var undefinedRole = command.AdministrationRoles.Where(x => !Enum.IsDefined(x)).Cast<AdministrationRole?>()
            .FirstOrDefault();
        if (undefinedRole is not null)
            return new InvalidInputError(nameof(UpdateAdministrationRolesCommand.AdministrationRoles), $"Role '{undefinedRole}' does not exist");

        return Result.Ok();
    }
}
