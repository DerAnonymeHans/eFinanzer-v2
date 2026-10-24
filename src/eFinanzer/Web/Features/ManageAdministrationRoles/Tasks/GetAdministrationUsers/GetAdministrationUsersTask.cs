using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;

public interface IGetAdministrationUsersTask
{
    Task<Result<GetAdministrationUsersDto>> GetUsers(CancellationToken ct);
}

public class GetAdministrationUsersTask(
    IKeycloakUserClient keycloakUserClient,
    ILogger<GetAdministrationUsersTask> logger
) : IGetAdministrationUsersTask
{
    /// <summary>
    /// Maps the Keycloak group name of a role (e.g. <c>head-finance-officer</c>) back to the role
    /// </summary>
    private static readonly Dictionary<string, AdministrationRole> RolesByGroupName = Enum
        .GetValues<AdministrationRole>()
        .ToDictionary(KeycloakGroups.GetGroupName);

    public async Task<Result<GetAdministrationUsersDto>> GetUsers(CancellationToken ct)
    {
        var getRolesWithKeycloakGroupsResult = await GetRolesWithKeycloakGroups(ct);
        if (getRolesWithKeycloakGroupsResult.IsError)
            return getRolesWithKeycloakGroupsResult.Error;

        var members = await Task.WhenAll(getRolesWithKeycloakGroupsResult.Value.Select(async x =>
            (x.Group, x.Role, Users: await keycloakUserClient.GetUsersInGroup(x.Group.Id, ct))));

        // A user can be in several role groups
        var rolesByUserName = new Dictionary<string, List<AdministrationRole>>();
        foreach (var (group, role, getUsersResult) in members)
        {
            if (getUsersResult.IsError)
            {
                logger.LogError("Loading the members of administration group {GroupPath} failed: {Error}",
                    group.Path, getUsersResult.Error);
                return new LoadAdministrationRoleMembersError(group.Path, getUsersResult.Error);
            }

            foreach (var user in getUsersResult.Value)
            {
                if (!rolesByUserName.TryGetValue(user.Username, out var roles))
                    rolesByUserName[user.Username] = roles = [];

                roles.Add(role);
            }
        }

        return new GetAdministrationUsersDto
        {
            AdministrationUsers = rolesByUserName
                .OrderBy(x => x.Key)
                .Select(x => new AdministrationUserDto
                {
                    Username = x.Key,
                    Roles = x.Value.Order().ToArray(),
                })
                .ToArray(),
        };
    }

    private async Task<Result<List<RoleWithKeycloakGroup>>> GetRolesWithKeycloakGroups(CancellationToken ct)
    {
        var getSubgroupsResult = await GetRoleGroups(ct);
        if (getSubgroupsResult.IsError)
        {
            logger.LogError("Loading the role groups of the administration failed: {Error}", getSubgroupsResult.Error);
            return new LoadAdministrationRoleGroupsError(getSubgroupsResult.Error);
        }

        // Every subgroup of the administration group stands for one role
        var roleGroups = new List<RoleWithKeycloakGroup>();
        foreach (var subgroup in getSubgroupsResult.Value)
        {
            if (!RolesByGroupName.TryGetValue(subgroup.Name, out var role))
            {
                logger.LogWarning("Ignoring administration group {GroupPath}, it is no known role", subgroup.Path);
                continue;
            }
            roleGroups.Add(new(role, subgroup));
        }

        return roleGroups;
    }

    private async Task<Result<List<KeycloakGroup>>> GetRoleGroups(CancellationToken ct)
    {
        var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.AdministrationGroup, ct);
        if (groupIdResult.IsError)
            return groupIdResult.Error;

        return await keycloakUserClient.GetSubgroups(groupIdResult.Value, ct);
    }

    record RoleWithKeycloakGroup(AdministrationRole Role, KeycloakGroup Group);
}
