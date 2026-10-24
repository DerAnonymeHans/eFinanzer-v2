/** A role, e.g. an AdministrationRole or an AccountingUnitRole */
export type RoleKey = string;

export type UserWithRoles = {
	username: string;
	roles: RoleKey[];
};
