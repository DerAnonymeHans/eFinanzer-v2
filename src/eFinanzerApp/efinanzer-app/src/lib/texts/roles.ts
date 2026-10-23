import type { AccountingUnitRole, AdministrationRole, UserAccess } from '../api/types';

// Display names of all roles. Typed as Record, so a role added in the API fails the type check until it has a name here.
// The key order is the display order, most important first.
export const administrationRoleNames: Record<AdministrationRole, string> = {
	HeadFinanceOfficer: 'StuRa-Finanzer',
	FinanceAdministrator: 'Finanzverwaltung',
	FinanceReviewer: 'Finanzprüfung',
};

export const accountingUnitRoleNames: Record<AccountingUnitRole, string> = {
	FinanceOfficer: 'Finanzer',
	Treasurer: 'Kassenwart',
	FinanceAuthorizedUser: 'Finanzbevollmächtigter',
};

/** Name of a role within an accounting unit, e.g. "FSR DIT · Kassenwart" */
export function formatAccountingUnitRole(accountingUnitName: string, role: AccountingUnitRole): string {
	return `${accountingUnitName} · ${accountingUnitRoleNames[role]}`;
}

function byDisplayOrder<TRole extends string>(names: Record<TRole, string>) {
	const order = Object.keys(names);
	return (a: TRole, b: TRole) => order.indexOf(a) - order.indexOf(b);
}

/** All role names of a user: administration roles first, then the roles in the accounting units */
export function getRoleNames(userAccess: UserAccess): string[] {
	const administration = [...userAccess.administrationRoles]
		.sort(byDisplayOrder(administrationRoleNames))
		.map((role) => administrationRoleNames[role]);

	const accountingUnits = userAccess.accountingUnits.flatMap((unit) =>
		[...unit.accountingUnitRoles]
			.sort(byDisplayOrder(accountingUnitRoleNames))
			.map((role) => formatAccountingUnitRole(unit.name ?? unit.key, role)),
	);

	return [...administration, ...accountingUnits];
}
