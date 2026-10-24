import { expect, test } from 'vitest';
import type { UserAccess } from '../api/types';
import { getRoleNames } from './roles';

test('lists administration roles first, then the roles per accounting unit', () => {
	const userAccess: UserAccess = {
		administrationRoles: ['FinanceReviewer', 'HeadFinanceOfficer'],
		accountingUnits: [
			{ key: 'fsr-dit', name: 'FSR DIT', accountingUnitRoles: ['Treasurer', 'FinanceOfficer'], fiscalYears: [] },
			{ key: 'fsr-im', name: null, accountingUnitRoles: ['FinanceAuthorizedUser'], fiscalYears: [] },
		],
	};

	expect(getRoleNames(userAccess)).toEqual([
		'Haupt-Finanzer',
		'Finanzprüfung',
		'FSR DIT · Finanzer',
		'FSR DIT · Kassenwart',
		'fsr-im · Finanzbevollmächtigter',
	]);
});

test('returns no names without roles', () => {
	expect(getRoleNames({ administrationRoles: [], accountingUnits: [] })).toEqual([]);
});
