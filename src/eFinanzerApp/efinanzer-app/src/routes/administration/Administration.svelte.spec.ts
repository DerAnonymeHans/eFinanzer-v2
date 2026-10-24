import { createRawSnippet, mount, unmount } from 'svelte';
import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page, userEvent } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import apiClient from '#lib/api/client.js';
import type { AdministrationUser, RoleChangeRequest, UserAccess } from '#lib/api/types.js';
import { setAuth } from '#lib/auth/auth.svelte.js';
import { failed, ok, renderAppOverlays, resetApp } from '../../test-utils';
import AdministrationLayout from './+layout.svelte';
import AdministrationPage from './+page.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const administratorAccess: UserAccess = {
	administrationRoles: ['FinanceAdministrator'],
	accountingUnits: [],
	isAdministration: true,
	resolveRoleChangeRequests: true,
	manageAdministrationRoles: true
};

/** May see the administration, but neither resolve requests nor change roles */
const reviewerAccess: UserAccess = {
	administrationRoles: ['FinanceReviewer'],
	accountingUnits: [],
	isAdministration: true
};

const fullNames: Record<string, [string, string]> = {
	mmuster: ['Max', 'Muster'],
	eadmin: ['Erika', 'Admin'],
	rpruef: ['Rita', 'Prüfer'],
	nneu: ['Rina', 'Neu']
};

const request: RoleChangeRequest = {
	id: 7,
	requestedBy: 'mmuster',
	createdAt: '2026-10-01T10:00:00Z',
	requestComment: 'Neu gewählt',
	roleChanges: [
		{ accountingUnitKey: 'fsr-dit', accountingUnitRole: 'Treasurer', isAdded: true },
		{ accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: false }
	]
};

const pendingPath = '/api/manage-role-change-requests/list-pending-role-change-requests';
const administrationUsersPath = '/api/manage-administration-roles/get-administration-users';
const updateRolesPath = '/api/manage-administration-roles/update-administration-roles';

let administrationUsers: AdministrationUser[] = [];

function mockPending(requests: RoleChangeRequest[], totalCount = requests.length) {
	api.GET.mockImplementation((path: string, options?: { params: { query: Record<string, string> } }) => {
		if (path === pendingPath) return Promise.resolve(ok({ items: requests, totalCount }));
		if (path === administrationUsersPath) return Promise.resolve(ok({ administrationUsers }));
		if (path === '/api/manage-role-change-requests/list-accounting-units') {
			return Promise.resolve(
				ok([
					{ key: 'fsr-dit', name: 'FSR DIT' },
					{ key: 'fsr-im', name: 'FSR IM' }
				])
			);
		}
		if (path === '/api/get-users/get-user') {
			const username = options!.params.query.username;
			const [firstName, lastName] = fullNames[username] ?? [];
			return Promise.resolve(firstName ? ok({ username, firstName, lastName }) : failed(404));
		}
		if (path === '/api/get-users/search-user') {
			const search = options!.params.query.search.toLowerCase();
			const found = Object.entries(fullNames)
				.filter(([, names]) => names.join(' ').toLowerCase().includes(search))
				.map(([username, [firstName, lastName]]) => ({ username, firstName, lastName }));
			return Promise.resolve(ok(found));
		}
		if (path === '/api/get-user-access/my-user-access') return Promise.resolve(ok(administratorAccess));
		return Promise.resolve(failed(404));
	});
}

/** The page inside its access guard, like at /administration */
const pageSnippet = createRawSnippet(() => ({
	render: () => '<div></div>',
	setup: (target) => {
		const component = mount(AdministrationPage, { target });
		return () => void unmount(component);
	}
}));

/** Logged in as someone else than the requesting user, unless the test says otherwise */
function renderPage(userAccess: UserAccess = administratorAccess, username = 'admin') {
	resetApp(userAccess);
	setAuth({ username, displayName: username === 'admin' ? 'Ada Admin' : 'Max Muster', logout: vi.fn() });
	renderAppOverlays();
	return render(AdministrationLayout, { children: pageSnippet } as never);
}

/** The button of a page number. Its label comes from bits-ui, so only the number is matched. */
function pageButton(number: number) {
	return page.getByRole('button', { name: new RegExp(`\\b${number}$`) });
}

function requestRow() {
	return page.getByRole('row').filter({ hasText: 'Max Muster' });
}

function administrationUserRow(fullName: string) {
	return page.getByRole('row').filter({ hasText: fullName });
}

function editButton(fullName: string) {
	return page.getByRole('button', { name: `Rollen von ${fullName} bearbeiten` });
}

/** Opens the user search of the dialog, types into it and waits for the search request */
async function searchUser(text: string) {
	const dialog = page.getByRole('dialog');
	// The trigger, the search input inside the opened popover has the same name
	await userEvent.click(dialog.getByRole('combobox', { name: 'Benutzer suchen' }).first());
	await userEvent.fill(page.getByPlaceholder('Name, Benutzername oder E-Mail'), text);
	await expect
		.poll(() => api.GET.mock.calls)
		.toContainEqual(['/api/get-users/search-user', { params: { query: { search: text } } }]);
}

function administrationUsersLoads() {
	return api.GET.mock.calls.filter(([path]) => path === administrationUsersPath).length;
}

beforeEach(async () => {
	// Other names than the requesting user, so the rows of both tables can be told apart
	administrationUsers = [
		{ username: 'eadmin', roles: ['FinanceAdministrator', 'HeadFinanceOfficer'] },
		{ username: 'rpruef', roles: ['FinanceReviewer'] }
	];
	mockPending([request]);
	await page.viewport(1280, 800);
});

test('denies access without an administration role', async () => {
	renderPage({ administrationRoles: [], accountingUnits: [], isAdministration: false });

	await expect.element(page.getByText('Kein Zugriff')).toBeInTheDocument();
	expect(api.GET).not.toHaveBeenCalledWith(pendingPath, expect.anything());
	expect(api.GET).not.toHaveBeenCalledWith(administrationUsersPath);
});

test('shows the administration users with their roles in display order', async () => {
	renderPage();

	await expect.element(administrationUserRow('Erika Admin')).toHaveTextContent(/Haupt-Finanzer\s*Finanzverwaltung/);
	await expect.element(administrationUserRow('Rita Prüfer')).toHaveTextContent('Finanzprüfung');
});

test('says so when the administration has no users', async () => {
	administrationUsers = [];
	renderPage();

	await expect.element(page.getByText('Keine Mitglieder in der Administration')).toBeInTheDocument();
});

test('shows an error for the administration users and loads them again on request', async () => {
	api.GET.mockImplementation((path: string) => Promise.resolve(path === administrationUsersPath ? failed(500) : failed(404)));
	renderPage(reviewerAccess);

	await expect
		.element(page.getByText('Mitglieder der Administration konnten nicht geladen werden (HTTP 500)'))
		.toBeInTheDocument();

	mockPending([]);
	await userEvent.click(page.getByRole('button', { name: 'Erneut versuchen' }));
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
});

test('shows the administration users without edit buttons to users who may not change roles', async () => {
	renderPage(reviewerAccess);

	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	await expect.element(page.getByRole('button', { name: /bearbeiten/ })).not.toBeInTheDocument();
	await expect.element(page.getByRole('button', { name: /entfernen/ })).not.toBeInTheDocument();
	await expect.element(page.getByRole('button', { name: 'Mitglied hinzufügen' })).not.toBeInTheDocument();
	// Neither may they resolve requests
	expect(api.GET).not.toHaveBeenCalledWith(pendingPath, expect.anything());
});

test('changes the roles of an administration user', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	const loadsBefore = administrationUsersLoads();

	await userEvent.click(editButton('Rita Prüfer'));
	const dialog = page.getByRole('dialog');
	await expect.element(dialog.getByRole('checkbox', { name: 'Finanzprüfung' })).toBeChecked();
	// Nothing changed yet
	await expect.element(dialog.getByRole('button', { name: 'Speichern' })).toBeDisabled();
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Finanzverwaltung' }));
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([
			updateRolesPath,
			{ body: { username: 'rpruef', administrationRoles: ['FinanceAdministrator', 'FinanceReviewer'] } }
		]);
	await expect.element(page.getByText('Rollen wurden gespeichert')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	await expect.poll(administrationUsersLoads).toBeGreaterThan(loadsBefore);
	// Only the roles of another user changed
	expect(api.GET).not.toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('warns that a user without roles is removed from the administration', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();

	await userEvent.click(editButton('Rita Prüfer'));
	const dialog = page.getByRole('dialog');
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Finanzprüfung' }));

	await expect.element(dialog.getByText('Ohne Rolle wird der Benutzer entfernt.')).toBeInTheDocument();
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));
	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([updateRolesPath, { body: { username: 'rpruef', administrationRoles: [] } }]);
});

test('reloads the own user access after changing the own roles', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage(administratorAccess, 'eadmin');

	await userEvent.click(editButton('Erika Admin'));
	await userEvent.click(page.getByRole('dialog').getByRole('checkbox', { name: 'Haupt-Finanzer' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Speichern' }));

	await expect.poll(() => api.GET.mock.calls).toContainEqual(['/api/get-user-access/my-user-access']);
});

test('keeps the dialog open if the user does not exist anymore', async () => {
	api.PUT.mockResolvedValue(failed(400, { type: 'UnknownUserError', userName: 'rpruef' }));
	renderPage();

	await userEvent.click(editButton('Rita Prüfer'));
	await userEvent.click(page.getByRole('dialog').getByRole('checkbox', { name: 'Haupt-Finanzer' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Speichern' }));

	await expect.element(page.getByText('Den Benutzer Rita Prüfer gibt es nicht mehr')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).toBeInTheDocument();
});

test('keeps the dialog open if the logged-in user removes their own managing role', async () => {
	api.PUT.mockResolvedValue(failed(400, { type: 'InvalidInputError', field: 'AdministrationRoles', message: 'Not allowed' }));
	renderPage(administratorAccess, 'eadmin');

	await userEvent.click(editButton('Erika Admin'));
	await userEvent.click(page.getByRole('dialog').getByRole('checkbox', { name: 'Finanzverwaltung' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Speichern' }));

	await expect.element(page.getByText('Du kannst dir die Rolle Finanzverwaltung nicht selbst entziehen')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).toBeInTheDocument();
});

test('closes the dialog and reloads if the roles were saved only partly', async () => {
	api.PUT.mockResolvedValue(failed(500, { type: 'ChangeUserRoleError', userName: 'rpruef', role: 'HeadFinanceOfficer', added: true }));
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	const loadsBefore = administrationUsersLoads();

	await userEvent.click(editButton('Rita Prüfer'));
	await userEvent.click(page.getByRole('dialog').getByRole('checkbox', { name: 'Haupt-Finanzer' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Speichern' }));

	await expect
		.element(page.getByText('Rollen konnten nicht vollständig gespeichert werden: Die Rolle Haupt-Finanzer konnte nicht hinzugefügt werden'))
		.toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	await expect.poll(administrationUsersLoads).toBeGreaterThan(loadsBefore);
});

test('keeps the dialog open if nothing was saved', async () => {
	api.PUT.mockResolvedValue(failed(403));
	renderPage();

	await userEvent.click(editButton('Rita Prüfer'));
	await userEvent.click(page.getByRole('dialog').getByRole('checkbox', { name: 'Haupt-Finanzer' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Speichern' }));

	await expect.element(page.getByText('Rollen konnten nicht gespeichert werden (HTTP 403)')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).toBeInTheDocument();
});

test('shows the pending requests of the first page', async () => {
	renderPage();

	await expect.element(page.getByText('Max Muster')).toBeInTheDocument();
	await expect.element(page.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
	await expect.element(page.getByText('FSR IM · Finanzer')).toBeInTheDocument();
	await expect.element(page.getByText('Neu gewählt')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith(pendingPath, { params: { query: { PageSize: 20, Offset: 0 } } });
});

test('says so when there are no pending requests', async () => {
	mockPending([]);
	renderPage();

	await expect.element(page.getByText('Keine offenen Rollenänderungsanträge')).toBeInTheDocument();
});

test('loads the next page', async () => {
	mockPending([request], 25);
	renderPage();

	await userEvent.click(pageButton(2));

	await expect
		.poll(() => api.GET.mock.calls)
		.toContainEqual([pendingPath, { params: { query: { PageSize: 20, Offset: 20 } } }]);
});

test('starts on the first page with another page size', async () => {
	mockPending([request], 25);
	renderPage();

	await userEvent.click(pageButton(2));
	await userEvent.click(page.getByRole('combobox', { name: 'Einträge pro Seite' }));
	await userEvent.click(page.getByRole('option', { name: '50 / Seite' }));

	await expect
		.poll(() => api.GET.mock.calls)
		.toContainEqual([pendingPath, { params: { query: { PageSize: 50, Offset: 0 } } }]);
});

test('shows the page before after the last request of a page was resolved', async () => {
	const lastRequest = { ...request, id: 8 };
	mockPending([lastRequest], 21);
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();
	await userEvent.click(pageButton(2));
	await expect.element(pageButton(2)).toHaveAttribute('aria-current', 'page');

	// Page 2 is empty afterwards, so page 1 is loaded instead
	mockPending([request], 20);
	await userEvent.click(requestRow().getByRole('button', { name: 'Ablehnen' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Ablehnen' }));

	await expect
		.poll(() => api.GET.mock.calls.filter(([path]) => path === pendingPath).at(-1))
		.toEqual([pendingPath, { params: { query: { PageSize: 20, Offset: 0 } } }]);
	await expect.element(pageButton(1)).toHaveAttribute('aria-current', 'page');
});

test('shows an error and loads again on request', async () => {
	api.GET.mockImplementation((path: string) => {
		if (path === pendingPath) return Promise.resolve(failed(500));
		if (path === administrationUsersPath) return Promise.resolve(ok({ administrationUsers }));
		return Promise.resolve(ok([]));
	});
	renderPage();

	await expect
		.element(page.getByText('Offene Rollenänderungsanträge konnten nicht geladen werden (HTTP 500)'))
		.toBeInTheDocument();

	mockPending([request]);
	await userEvent.click(page.getByRole('button', { name: 'Erneut versuchen' }));
	await expect.element(page.getByText('Max Muster')).toBeInTheDocument();
});

test('approves a request with a comment', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();

	await userEvent.click(requestRow().getByRole('button', { name: 'Annehmen' }));
	const dialog = page.getByRole('dialog');
	await userEvent.fill(dialog.getByLabelText(/Kommentar/), 'Willkommen');
	await userEvent.click(dialog.getByRole('button', { name: 'Annehmen' }));

	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([
			'/api/manage-role-change-requests/resolve-role-change-request',
			{ params: { query: { id: 7 } }, body: { resolution: 'Approved', comment: 'Willkommen' } }
		]);
	await expect.element(page.getByText('Antrag wurde angenommen')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	// Only the roles of the requesting user changed
	expect(api.GET).not.toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('reloads the own user access after resolving an own request', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage(administratorAccess, 'mmuster');

	await userEvent.click(requestRow().getByRole('button', { name: 'Annehmen' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Annehmen' }));

	await expect.poll(() => api.GET.mock.calls).toContainEqual(['/api/get-user-access/my-user-access']);
});

test('dismisses a request without a comment', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();

	await userEvent.click(requestRow().getByRole('button', { name: 'Ablehnen' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Ablehnen' }));

	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([
			'/api/manage-role-change-requests/resolve-role-change-request',
			{ params: { query: { id: 7 } }, body: { resolution: 'Dismissed', comment: null } }
		]);
	await expect.element(page.getByText('Antrag wurde abgelehnt')).toBeInTheDocument();
});

test('keeps the dialog open if the request cannot be applied', async () => {
	api.PUT.mockResolvedValue(failed(409, { type: 'RoleChangeRequestNotApplicableError', id: 7, reason: 'user does not exist anymore' }));
	renderPage();

	await userEvent.click(requestRow().getByRole('button', { name: 'Annehmen' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Annehmen' }));

	await expect.element(page.getByText(/kann nicht angewendet werden/)).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).toBeInTheDocument();
});

test('closes the dialog and reloads if the request was already resolved', async () => {
	api.PUT.mockResolvedValue(failed(409));
	renderPage();
	await expect.element(page.getByText('Max Muster')).toBeInTheDocument();
	const loadsBefore = api.GET.mock.calls.filter(([path]) => path === pendingPath).length;

	await userEvent.click(requestRow().getByRole('button', { name: 'Ablehnen' }));
	await userEvent.click(page.getByRole('dialog').getByRole('button', { name: 'Ablehnen' }));

	await expect.element(page.getByText('Der Antrag wurde bereits entschieden')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	await expect
		.poll(() => api.GET.mock.calls.filter(([path]) => path === pendingPath).length)
		.toBeGreaterThan(loadsBefore);
});

test('adds a new member to the administration', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	const loadsBefore = administrationUsersLoads();

	await userEvent.click(page.getByRole('button', { name: 'Mitglied hinzufügen' }));
	const dialog = page.getByRole('dialog');
	const addButton = dialog.getByRole('button', { name: 'Hinzufügen' });
	await expect.element(addButton).toBeDisabled();
	await searchUser('Rina');
	await userEvent.click(page.getByRole('option', { name: 'Rina Neu' }));
	// Without a role nothing would be added
	await expect.element(addButton).toBeDisabled();
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Finanzprüfung' }));
	await userEvent.click(addButton);

	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([updateRolesPath, { body: { username: 'nneu', administrationRoles: ['FinanceReviewer'] } }]);
	await expect.element(page.getByText('Mitglied wurde hinzugefügt')).toBeInTheDocument();
	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	await expect.poll(administrationUsersLoads).toBeGreaterThan(loadsBefore);
});

test('leaves the members out of the search for a new member', async () => {
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();

	await userEvent.click(page.getByRole('button', { name: 'Mitglied hinzufügen' }));
	// Matches the members Rita Prüfer and Erika Admin and Rina Neu, who is no member
	await searchUser('ri');

	await expect.element(page.getByRole('option', { name: 'Rina Neu' })).toBeInTheDocument();
	await expect.element(page.getByRole('option', { name: 'Rita Prüfer' })).not.toBeInTheDocument();
	await expect.element(page.getByRole('option', { name: 'Erika Admin' })).not.toBeInTheDocument();
});

test('removes a member from the administration only after confirming', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	const loadsBefore = administrationUsersLoads();

	await userEvent.click(page.getByRole('button', { name: 'Rita Prüfer entfernen' }));
	const dialog = page.getByRole('alertdialog');
	await expect.element(dialog).toHaveTextContent('Rita Prüfer aus der Administration entfernen?');
	expect(api.PUT).not.toHaveBeenCalled();
	await userEvent.click(dialog.getByRole('button', { name: 'Entfernen' }));

	await expect
		.poll(() => api.PUT.mock.calls)
		.toContainEqual([updateRolesPath, { body: { username: 'rpruef', administrationRoles: [] } }]);
	await expect.element(page.getByText('Rita Prüfer wurde aus der Administration entfernt')).toBeInTheDocument();
	await expect.poll(administrationUsersLoads).toBeGreaterThan(loadsBefore);
	// Only the roles of another user changed
	expect(api.GET).not.toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('removes nothing if removing is not confirmed', async () => {
	renderPage();

	await userEvent.click(page.getByRole('button', { name: 'Rita Prüfer entfernen' }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Abbrechen' }));

	await expect.element(page.getByRole('alertdialog')).not.toBeInTheDocument();
	expect(api.PUT).not.toHaveBeenCalled();
});

test('reloads the own user access after removing oneself', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage(administratorAccess, 'eadmin');

	await userEvent.click(page.getByRole('button', { name: 'Erika Admin entfernen' }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Entfernen' }));

	await expect.poll(() => api.GET.mock.calls).toContainEqual(['/api/get-user-access/my-user-access']);
});

test('reloads the members if removing failed partly', async () => {
	api.PUT.mockResolvedValue(failed(500, { type: 'ChangeUserRoleError', userName: 'rpruef', role: 'FinanceReviewer', added: false }));
	renderPage();
	await expect.element(administrationUserRow('Rita Prüfer')).toBeInTheDocument();
	const loadsBefore = administrationUsersLoads();

	await userEvent.click(page.getByRole('button', { name: 'Rita Prüfer entfernen' }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Entfernen' }));

	await expect
		.element(page.getByText('Rollen konnten nicht vollständig entfernt werden: Die Rolle Finanzprüfung konnte nicht entfernt werden'))
		.toBeInTheDocument();
	await expect.poll(administrationUsersLoads).toBeGreaterThan(loadsBefore);
});
