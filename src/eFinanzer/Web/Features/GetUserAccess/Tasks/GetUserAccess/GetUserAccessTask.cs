using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Database;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess;

public interface IGetUserAccessTask
{
    Task<Result<UserAccess>> Get(ClaimsPrincipal user, CancellationToken ct);
}

public class GetUserAccessTask(
    EFinanzerDbContext context
    ) : IGetUserAccessTask
{
    private const string AccountingUnitGroupPrefix = $"{KeycloakGroups.AccountingUnitsGroup}/";
    private const string AdministrationGroupPrefix = $"{KeycloakGroups.AdministrationGroup}/";

    /// <summary>
    /// Maps the Keycloak group name of a role (e.g. <c>finance-officer</c>) back to the role
    /// </summary>
    private static readonly Dictionary<string, AccountingUnitRole> AccountingUnitRolesByGroupName = Enum
        .GetValues<AccountingUnitRole>()
        .ToDictionary(KeycloakGroups.GetGroupName);

    private static readonly Dictionary<string, AdministrationRole> AdministrationRolesByGroupName = Enum
        .GetValues<AdministrationRole>()
        .ToDictionary(KeycloakGroups.GetGroupName);

    public async Task<Result<UserAccess>> Get(ClaimsPrincipal user, CancellationToken ct)
    {
        // The token contains one "groups" claim per group the user is a member of
        var groups = user.FindAll(UserClaims.GroupClaimType)
            .Select(x => x.Value)
            .ToHashSet();
        if (groups.Count == 0)
            return UserAccess.NoAccess();

        var administrationRoles = GetAdministrationRoles(groups);
        var accountingUnitRolesByKey = GetAccountingUnitRoles(groups);
        var manageAccountingUnits = GroupRules.GetManageAccountingUnitGroups().Any(groups.Contains);
        var resolveRoleChangeRequests = GroupRules.GetResolveRoleChangeRequestsGroups().Any(groups.Contains);

        var query = context.AccountingUnits.AsQueryable();
        if (!manageAccountingUnits)
        {
            var accountingUnitKeys = accountingUnitRolesByKey.Keys.ToList();
            query = query.Where(x => accountingUnitKeys.Contains(x.Key));
        }

        var accountingUnits = await query
            .OrderBy(x => x.Key)
            .Select(x => new
            {
                x.Key,
                x.Name,
                FiscalYears = x.FiscalYears!
                    .OrderByDescending(f => f.StartDate)
                    .Select(f => new UserAccess.FiscalYearDto { Key = f.Key, Name = f.Name })
                    .ToArray(),
            })
            .ToListAsync(ct);

        return new UserAccess
        {
            ManageAccountingUnits = manageAccountingUnits,
            ResolveRoleChangeRequests = resolveRoleChangeRequests,
            AdministrationRoles = administrationRoles,
            AccountingUnits = accountingUnits
                .Select(x => new UserAccess.AccountingUnitDto
                {
                    Key = x.Key,
                    Name = x.Name,
                    ManageFiscalYears = GroupRules.GetManageFiscalYearGroups(x.Key).Any(groups.Contains),
                    // Administrators see every accounting unit, also those they have no role in
                    AccountingUnitRoles = accountingUnitRolesByKey.GetValueOrDefault(x.Key) ?? [],
                    FiscalYears = x.FiscalYears,
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Administration roles of the user, taken from groups like <c>/administration/{role}</c>
    /// </summary>
    private static AdministrationRole[] GetAdministrationRoles(IEnumerable<string> groups) => groups
        .Where(x => x.StartsWith(AdministrationGroupPrefix, StringComparison.Ordinal))
        .Select(x => AdministrationRolesByGroupName.TryGetValue(x[AdministrationGroupPrefix.Length..], out var role)
            ? role
            : (AdministrationRole?)null)
        .OfType<AdministrationRole>()
        .Distinct()
        .Order()
        .ToArray();

    /// <summary>
    /// Roles of the user per accounting unit key, taken from groups like <c>/accounting-units/{key}/{role}</c>
    /// </summary>
    private static Dictionary<string, AccountingUnitRole[]> GetAccountingUnitRoles(IEnumerable<string> groups) => groups
        .Where(x => x.StartsWith(AccountingUnitGroupPrefix, StringComparison.Ordinal))
        .Select(x => x[AccountingUnitGroupPrefix.Length..].Split('/'))
        .Where(x => x is [{ Length: > 0 }, _])
        .Select(x => (Key: x[0], Found: AccountingUnitRolesByGroupName.TryGetValue(x[1], out var role), Role: role))
        .Where(x => x.Found)
        .GroupBy(x => x.Key, x => x.Role)
        .ToDictionary(x => x.Key, x => x.Distinct().Order().ToArray());
}
