using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;

public interface IGetAccountingUnitTask
{
    Task<Result<GetAccountingUnitManagementDto>> Get(string key, CancellationToken ct);
    Task<Result<List<GetAccountingUnitManagementListDto>>> GetAll(CancellationToken ct);
}

public class GetAccountingUnitTask(
    EFinanzerDbContext context,
    IKeycloakUserClient keycloakUserClient,
    ILogger<GetAccountingUnitTask> logger
    ) : IGetAccountingUnitTask
{
    /// <summary>
    /// Maps the Keycloak group name of a role (e.g. <c>finance-officer</c>) back to the role
    /// </summary>
    private static readonly Dictionary<string, AccountingUnitRole> RolesByGroupName = Enum
        .GetValues<AccountingUnitRole>()
        .ToDictionary(KeycloakGroups.GetGroupName);

    public async Task<Result<GetAccountingUnitManagementDto>> Get(string key, CancellationToken ct)
    {
        var accountingUnit = await context.AccountingUnits
            .Where(x => x.Key == key)
            .Select(x => new { x.Key, x.Name, x.Description })
            .FirstOrDefaultAsync(ct);

        if (accountingUnit is null)
            return new NotFoundError(nameof(AccountingUnit), key);

        var getRolesWithKeycloakGroupsResult = await GetRolesWithKeycloakGroups(key, ct);
        if (getRolesWithKeycloakGroupsResult.IsError)
            return getRolesWithKeycloakGroupsResult.Error;

        var members = await Task.WhenAll(getRolesWithKeycloakGroupsResult.Value.Select(async x =>
            (x.Group, x.Role, Users: await keycloakUserClient.GetUsersInGroup(x.Group.Id, ct))));

        // A user can be in several role groups
        var rolesByUserName = new Dictionary<string, List<AccountingUnitRole>>();
        foreach (var (group, role, getUsersResult) in members)
        {
            if (getUsersResult.IsError)
            {
                logger.LogError("Loading the members of group {GroupPath} of accounting unit {AccountingUnitKey} failed: {Error}",
                    group.Path, key, getUsersResult.Error);
                return new LoadAccountingUnitRoleMembersError(key, group.Path, getUsersResult.Error);
            }

            foreach (var user in getUsersResult.Value)
            {
                if (!rolesByUserName.TryGetValue(user.Username, out var roles))
                    rolesByUserName[user.Username] = roles = [];

                roles.Add(role);
            }
        }

        return new GetAccountingUnitManagementDto
        {
            Key = accountingUnit.Key,
            Name = accountingUnit.Name,
            Description = accountingUnit.Description,
            UsersWithRoles = rolesByUserName
                .OrderBy(x => x.Key)
                .Select(x => new GetAccountingUnitManagementDto.UserDto
                {
                    UserName = x.Key,
                    Roles = x.Value.Order().ToArray(),
                })
                .ToArray(),
        };
    }

    private async Task<Result<List<RoleWithKeycloakGroup>>> GetRolesWithKeycloakGroups(string key, CancellationToken ct)
    {
        var getSubgroupsResult = await GetRoleGroups(key, ct);
        if (getSubgroupsResult.IsError)
        {
            logger.LogError("Loading the role groups of accounting unit {AccountingUnitKey} failed: {Error}",
                key, getSubgroupsResult.Error);
            return new LoadAccountingUnitRoleGroupsError(key, getSubgroupsResult.Error);
        }

        // Every subgroup of the accounting unit stands for one role
        var roleGroups = new List<RoleWithKeycloakGroup>();
        foreach (var subgroup in getSubgroupsResult.Value)
        {
            if (!RolesByGroupName.TryGetValue(subgroup.Name, out var role))
            {
                logger.LogWarning("Ignoring group {GroupPath} of accounting unit {AccountingUnitKey}, it is no known role",
                    subgroup.Path, key);
                continue;
            }
            roleGroups.Add(new(role, subgroup));
        }

        return roleGroups;
    }

    private async Task<Result<List<KeycloakGroup>>> GetRoleGroups(string key, CancellationToken ct)
    {
        var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(KeycloakGroups.GetAccountingUnitGroup(key), ct);
        if (groupIdResult.IsError)
            return groupIdResult.Error;

        return await keycloakUserClient.GetSubgroups(groupIdResult.Value, ct);
    }

    public async Task<Result<List<GetAccountingUnitManagementListDto>>> GetAll(CancellationToken ct)
    {
        var accountingUnits = await context.AccountingUnits
            .OrderBy(x => x.Key)
            .Select(x => new GetAccountingUnitManagementListDto
            {
                Key = x.Key,
                Name = x.Name,
                Description = x.Description,
            })
            .ToListAsync(ct);

        return accountingUnits;
    }
    
    record RoleWithKeycloakGroup(AccountingUnitRole Role, KeycloakGroup Group);
}
