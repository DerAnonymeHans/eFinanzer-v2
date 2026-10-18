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
    public const string GroupClaimType = "groups";

    

    /// <summary>
    /// Whether the user is a direct member of the group. Membership in a subgroup does not count for its parent.
    /// </summary>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/editor</c></param>
    public static bool IsInGroup(this ClaimsPrincipal user, string groupPath)
        => user.HasClaim(GroupClaimType, groupPath);

    public static ClaimBuilder CreateClaimBuilder(this ClaimsPrincipal user) => new(user);

    /// <summary>
    /// A class instead of a record, so copies cannot share the collected requirements
    /// </summary>
    public class ClaimBuilder(ClaimsPrincipal user)
    {
        /// <summary>
        /// Each entry is one requirement: the user must be in at least one of its groups
        /// </summary>
        private readonly List<string[]> _mustHaveOneOfGroups = [];

        public ClaimsPrincipal User { get; } = user;
        public string? AccountingUnitKey { get; private set; }
        
        private string GetAccountingUnitGroup(AccountingUnitRole role) =>
            KeycloakGroups.GetAccountingUnitRoleGroup(AccountingUnitKey ?? "", role);
        
        private string GetAdministrationGroup(AdministrationRole role) =>
            KeycloakGroups.GetAdministrationRoleGroup(role);

        public void Validate()
        {
            foreach (var mustHaveOneOfGroup in _mustHaveOneOfGroups)
            {
                if (!mustHaveOneOfGroup.Any(User.IsInGroup))
                    throw new AccessForbiddenException(mustHaveOneOfGroup);
            }
        }

        public ClaimBuilder ForAccountingUnit(string key)
        {
            AccountingUnitKey = key;
            return this;
        }
        
        public ClaimBuilder MustBeOneOf(AdministrationRole[] administrationRoles, AccountingUnitRole[] accountingUnitRoles)
        {
            _mustHaveOneOfGroups.Add([
                .. accountingUnitRoles.Select(GetAccountingUnitGroup),
                .. administrationRoles.Select(GetAdministrationGroup)
            ]);
            return this;
        }

        public ClaimBuilder MustBe(AccountingUnitRole role) => MustBeAllOf(role);

        public ClaimBuilder MustBeAllOf(params AccountingUnitRole[] roles)
        {
            _mustHaveOneOfGroups.AddRange(roles.Select(x => new[] { GetAccountingUnitGroup(x) }));
            return this;
        }

        public ClaimBuilder MustBeOneOf(params AccountingUnitRole[] roles)
        {
            _mustHaveOneOfGroups.Add(roles.Select(GetAccountingUnitGroup).ToArray());
            return this;
        }
        
        
        public ClaimBuilder MustBe(AdministrationRole role) => MustBeAllOf(role);

        public ClaimBuilder MustBeAllOf(params AdministrationRole[] roles)
        {
            _mustHaveOneOfGroups.AddRange(roles.Select(x => new[] { GetAdministrationGroup(x) }));
            return this;
        }

        public ClaimBuilder MustBeOneOf(params AdministrationRole[] roles)
        {
            _mustHaveOneOfGroups.Add(roles.Select(GetAdministrationGroup).ToArray());
            return this;
        }
        
        public ClaimBuilder MustBeOneOf(params string[] groups)
        {
            _mustHaveOneOfGroups.Add(groups);
            return this;
        }
        
        public ClaimBuilder MustBeOneOf(IEnumerable<string> groups)
        {
            _mustHaveOneOfGroups.Add(groups.ToArray());
            return this;
        }
        
    }
}
