using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges.Models;

namespace eFinanzer.Common.Tasks.GetRoleChanges;

public interface IHandleRoleChangesTask
{
    Task<Result<RoleChange<TRole>[]>> GetRoleChanges<TRole>(
        IEnumerable<UserWithRoles<TRole>> rolesBefore,
        IEnumerable<UserWithRoles<TRole>> rolesAfter,
        Func<TRole, string> getKeycloakGroupFromRole,
        CancellationToken ct)
        where TRole : Enum;
    
    
    Task<Result> ExecuteRoleChanges<TRole>(IEnumerable<RoleChange<TRole>> roleChanges, CancellationToken ct)
        where TRole : Enum;
}

public class HandleRoleChangesTask(
    IKeycloakUserClient keycloakUserClient,
    ILogger<HandleRoleChangesTask> logger
    ) : IHandleRoleChangesTask
{
    private record UserRole<TRole>(string Username, TRole Role) where  TRole : Enum;

    /// <summary>
    /// Diffs the requested roles with the current ones in Keycloak and resolves the user and group ids needed to apply
    /// them
    /// </summary>
    public async Task<Result<RoleChange<TRole>[]>> GetRoleChanges<TRole>(
        IEnumerable<UserWithRoles<TRole>> rolesBefore, 
        IEnumerable<UserWithRoles<TRole>> rolesAfter, 
        Func<TRole, string> getKeycloakGroupFromRole,
        CancellationToken ct)
        where TRole : Enum
    {
        // Keycloak stores usernames in lower case
        var current = rolesBefore
            .SelectMany(user => user.Roles.Select(role => new UserRole<TRole>(user.Username.ToLowerInvariant(), role)))
            .ToHashSet();
        var requested = rolesAfter
            .SelectMany(user => user.Roles.Select(role => new UserRole<TRole>(user.Username.ToLowerInvariant(), role)))
            .ToHashSet();

        var added = requested.Except(current).ToList();
        var removed = current.Except(requested).ToList();

        var userIds = new Dictionary<string, string>();
        foreach (var userName in added.Concat(removed).Select(x => x.Username).Distinct())
        {
            var userResult = await keycloakUserClient.GetUserByUsername(userName, ct);
            if (userResult.IsError)
                return new UnknownUserError(userName);

            userIds[userName] = userResult.Value.Id;
        }

        var groupIds = new Dictionary<TRole, string>();
        foreach (var role in added.Concat(removed).Select(x => x.Role).Distinct())
        {
            var groupName = getKeycloakGroupFromRole(role);
            var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(groupName, ct);
            if (groupIdResult.IsError)
            {
                return new LoadCurrentUserRolesError(groupIdResult.Error);
            }

            groupIds[role] = groupIdResult.Value;
        }

        // Removals first, so a failure leaves users with fewer instead of more rights
        return removed.Select(x => new RoleChange<TRole>
            {
                Username = x.Username,
                UserId = userIds[x.Username],
                Role = x.Role,
                KeycloakGroupId = groupIds[x.Role],
                Type = RoleChangeType.Removed
            })
            .Concat(added.Select(x => new RoleChange<TRole>
            {
                Username = x.Username,
                UserId = userIds[x.Username],
                Role = x.Role,
                KeycloakGroupId = groupIds[x.Role],
                Type = RoleChangeType.Added
            }))
            .ToArray();
    }

    public async Task<Result> ExecuteRoleChanges<TRole>(IEnumerable<RoleChange<TRole>> roleChanges, CancellationToken ct) where TRole : Enum
    {
        foreach (var change in roleChanges)
        {
            var changeResult = change.Type is RoleChangeType.Added
                ? await keycloakUserClient.AddUserToGroup(change.UserId, change.KeycloakGroupId, ct)
                : await keycloakUserClient.RemoveUserFromGroup(change.UserId, change.KeycloakGroupId, ct);

            if (changeResult.IsError)
            {
                logger.LogError(
                    "Changing role {Role} (ChangeType: {ChangeType}) of user {UserName} failed: {Error}",
                    change.Role, change.Type, change.Username, changeResult.Error);
                return new ChangeUserRoleError(change.Username, change.Role, change.Type is RoleChangeType.Added, changeResult.Error);
            }

            logger.LogInformation(
                "Changed role {Role} (ChangeType: {ChangeType}) of user {UserName}",
                change.Role, change.Type, change.Username);
        }

        return Result.Ok();
    }
}