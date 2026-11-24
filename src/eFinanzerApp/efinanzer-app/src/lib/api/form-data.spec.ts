import { expect, test } from 'vitest';
import { toFormData } from './form-data.js';

test('names nested fields like the form binding of the backend', () => {
	const logo = new File(['png'], 'logo.png');

	const form = toFormData({
		name: 'FSR',
		description: null,
		count: 2,
		logo,
		usersWithRoles: [
			{ userName: 'mmuster', roles: ['Treasurer', 'FinanceOfficer'] },
			{ userName: 'eexample', roles: [] }
		]
	});

	expect([...form.entries()].map(([name, value]) => [name, value instanceof File ? value.name : value])).toEqual([
		['name', 'FSR'],
		['count', '2'],
		['logo', 'logo.png'],
		['usersWithRoles[0].userName', 'mmuster'],
		['usersWithRoles[0].roles[0]', 'Treasurer'],
		['usersWithRoles[0].roles[1]', 'FinanceOfficer'],
		['usersWithRoles[1].userName', 'eexample']
	]);
});
