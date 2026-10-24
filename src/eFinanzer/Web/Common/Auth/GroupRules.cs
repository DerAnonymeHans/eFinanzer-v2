using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Auth;

public static class GroupRules
{
    private static readonly string[] AdministrationGroups = Enum.GetValues<AdministrationRole>().Select(KeycloakGroups.GetAdministrationRoleGroup).ToArray();
    public static  IReadOnlyList<string> GetAdministrationRoleGroups() => AdministrationGroups;
    
    private static readonly string[] ManageAccountingUnitsGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator),
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer),
    ];
    public static IReadOnlyList<string> GetManageAccountingUnitGroups() => ManageAccountingUnitsGroups;

    
    private static readonly AccountingUnitRole[] ManageFiscalYearAccountingUnitRoles = [AccountingUnitRole.FinanceOfficer];
    private static readonly string[] ManageFiscalYearGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator),
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer),
    ];
    public static IReadOnlyList<string> GetManageFiscalYearGroups(string accountingUnitKey) =>
    [
        .. ManageFiscalYearGroups,
        .. ManageFiscalYearAccountingUnitRoles.Select(role => KeycloakGroups.GetAccountingUnitRoleGroup(accountingUnitKey, role))
    ];
    
    
    private static readonly string[] ResolveRoleChangeRequestsGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator),
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer),
    ];
    public static IReadOnlyList<string> GetResolveRoleChangeRequestsGroups() => ResolveRoleChangeRequestsGroups;


    private static readonly string[] ManageAdministrationRolesGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator)
    ];
    public static IReadOnlyList<string> GetManageAdministrationRolesGroups() => ManageAdministrationRolesGroups;
}