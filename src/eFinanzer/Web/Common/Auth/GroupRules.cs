using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;

namespace eFinanzer.Common.Auth;

public static class GroupRules
{
    private static readonly string[] ManageAccountingUnitsGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator),
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer),
    ];
    public static IReadOnlyList<string> GetManageAccountingUnitGroups() => ManageAccountingUnitsGroups;

    
    private static readonly AccountingUnitRole[] ManageFiscalYearAccountingUnitRoles = [AccountingUnitRole.FinanceOfficer];
    private static readonly string[] ManageFiscalYearGroups =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator)
    ];
    public static IReadOnlyList<string> GetManageFiscalYearGroups(string accountingUnitKey) =>
    [
        .. ManageFiscalYearGroups,
        .. ManageFiscalYearAccountingUnitRoles.Select(role => KeycloakGroups.GetAccountingUnitRoleGroup(accountingUnitKey, role))
    ];
}