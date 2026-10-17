using System.Security.Claims;
using System.Text.Json;
using eFinanzer.Common.Integrations.Keycloak.Constants;
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
        private string GetGroupPath(AccountingUnitRole role) =>
            KeycloakGroups.GetAccountingUnitRoleGroup(AccountingUnit, role);
        
        public AccountingUnitClaim MustBe(AccountingUnitRole role)
        {
            var groupPath = GetGroupPath(role);
            return !User.IsInGroup(groupPath) ? throw AccessForbiddenException.MustBeInAllGroups(groupPath) : this;
        }
        
        public AccountingUnitClaim MustBeAllOf(params AccountingUnitRole[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return !groupPaths.All(User.IsInGroup) ? throw AccessForbiddenException.MustBeInAllGroups(groupPaths) : this;
        }
        
        public AccountingUnitClaim MustBeOneOf(params AccountingUnitRole[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return groupPaths.Any(User.IsInGroup) ? this : throw AccessForbiddenException.MustBeInAnyGroups(groupPaths);
        }
    }
    
    
    public record AdministrationClaim(ClaimsPrincipal User)
    {
        private string GetGroupPath(AdministrationRole role) => KeycloakGroups.GetAdministrationRoleGroup(role);

        public AdministrationClaim MustBe(AdministrationRole role)
        {
            var groupPath = GetGroupPath(role);
            return !User.IsInGroup(groupPath) ? throw AccessForbiddenException.MustBeInAllGroups(groupPath) : this;
        }
        
        public AdministrationClaim MustBeAllOf(params AdministrationRole[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return !groupPaths.All(User.IsInGroup) ? throw AccessForbiddenException.MustBeInAllGroups(groupPaths) : this;
        }
        
        public AdministrationClaim MustBeOneOf(params AdministrationRole[] roles)
        {
            var groupPaths = roles.Select(GetGroupPath).ToArray();
            return groupPaths.Any(User.IsInGroup) ? this : throw AccessForbiddenException.MustBeInAnyGroups(groupPaths);
        }
    }
}
