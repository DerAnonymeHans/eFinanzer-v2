using System.Text.Json;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Integrations.Keycloak.Constants;

public static class KeycloakGroups
{
    public const string AccountingUnitsGroup = "/accounting-units";

    public static string GetAccountingUnitGroup(string key) => $"{AccountingUnitsGroup}/{key}";
    
    public static string GetAccountingUnitRoleGroup(string key, AccountingUnitRole role) => $"{GetAccountingUnitGroup(key)}/{GetGroupName(role)}";
    
    
    
    public const string AdministrationGroup = "/administration";
    
    public static string GetAdministrationRoleGroup(AdministrationRole role) => $"{AdministrationGroup}/{GetGroupName(role)}";

    /// <summary>
    /// Keycloak group name of a role, e.g. <c>HeadFinanceOfficer</c> → <c>head-finance-officer</c>
    /// </summary>
    public static string GetGroupName<TRole>(TRole role) where TRole : struct, Enum
        => JsonNamingPolicy.KebabCaseLower.ConvertName(role.ToString());
}