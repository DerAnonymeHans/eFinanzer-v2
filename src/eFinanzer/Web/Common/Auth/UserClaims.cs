using System.Security.Claims;
using System.Text.Json;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Auth;

/// <summary>
/// Checks the Keycloak group memberships of a user. Requires a "Group Membership" mapper with "Full group path"
/// enabled, which adds the claim <c>"groups": ["/accounting-units/fsr/editor", ...]</c> to the access token.
/// </summary>
public static class UserClaims
{
    public const string ClaimType = "groups";

    /// <summary>
    /// Keycloak group name of a role, e.g. <c>HeadFinanceOfficer</c> → <c>head-finance-officer</c>
    /// </summary>
    private static string GetGroupName<TRole>(TRole role) where TRole : struct, Enum
        => JsonNamingPolicy.KebabCaseLower.ConvertName(role.ToString());

    /// <summary>
    /// Whether the user is a direct member of the group. Membership in a subgroup does not count for its parent.
    /// </summary>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/editor</c></param>
    public static bool IsInGroup(this ClaimsPrincipal user, string groupPath)
        => user.HasClaim(ClaimType, groupPath);
    
    public static AccountingUnitClaim ForAccountingUnit(this ClaimsPrincipal user, string accountingUnit)
        => new(user, accountingUnit);
    
    public static AdministrationClaim ForAdministration(this ClaimsPrincipal user)
        => new(user);

    public record AccountingUnitClaim(ClaimsPrincipal User, string AccountingUnit)
    {
        private string GetGroupPath(AccountingUnitRoles role) =>
            $"/accounting-units/{AccountingUnit}/{GetGroupName(role)}";

        public AccountingUnitClaim MustBe(AccountingUnitRoles role)
        {
            var groupPath = GetGroupPath(role);
            return !User.IsInGroup(groupPath) ? throw AccessForbiddenException.MustBeInAllGroups(groupPath) : this;
        }
        
        public AccountingUnitClaim MustBeAllOf(params AccountingUnitRoles[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return !groupPaths.All(User.IsInGroup) ? throw AccessForbiddenException.MustBeInAllGroups(groupPaths) : this;
        }
        
        public AccountingUnitClaim MustBeOneOf(params AccountingUnitRoles[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return groupPaths.Any(User.IsInGroup) ? this : throw AccessForbiddenException.MustBeInAnyGroups(groupPaths);
        }
    }
    
    
    public record AdministrationClaim(ClaimsPrincipal User)
    {
        private string GetGroupPath(AdministrationRoles role) =>
            $"/administration/{GetGroupName(role)}";

        public AdministrationClaim MustBe(AdministrationRoles role)
        {
            var groupPath = GetGroupPath(role);
            return !User.IsInGroup(groupPath) ? throw AccessForbiddenException.MustBeInAllGroups(groupPath) : this;
        }
        
        public AdministrationClaim MustBeAllOf(params AdministrationRoles[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return !groupPaths.All(User.IsInGroup) ? throw AccessForbiddenException.MustBeInAllGroups(groupPaths) : this;
        }
        
        public AdministrationClaim MustBeOneOf(params AdministrationRoles[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return groupPaths.Any(User.IsInGroup) ? this : throw AccessForbiddenException.MustBeInAnyGroups(groupPaths);
        }
    }
}
