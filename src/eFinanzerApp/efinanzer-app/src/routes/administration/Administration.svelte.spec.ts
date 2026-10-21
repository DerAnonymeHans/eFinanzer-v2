import { createRawSnippet, mount, unmount } from 'svelte';
import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page, userEvent } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import apiClient from '#lib/api/client.js';
import type { RoleChangeRequest, UserAccess } from '#lib/api/types.js';
import { setAuth } from '#lib/auth/auth.svelte.js';
import { failed, ok, renderAppOverlays, resetApp } from '../../test-utils';
import AdministrationLayout from './+layout.svelte';
import AdministrationPage from './+page.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const administratorAccess: UserAccess = {
	administrationRoles: ['FinanceAdministrator'],
	accountingUnits: [],
	resolveRoleChangeRequests: true
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

function mockPending(requests: RoleChangeRequest[], totalCount = requests.length) {
	api.GET.mockImplementation((path: string) => {
		if (path === pendingPath) return Promise.resolve(ok({ items: requests, totalCount }));
		if (path === '/api/manage-role-change-requests/list-accounting-units') {
			return Promise.resolve(
				ok([
					{ key: 'fsr-dit', name: 'FSR DIT' },
					{ key: 'fsr-im', name: 'FSR IM' }
				])
			);
		}
		if (path === '/api/get-users/get-user') {
			return Promise.resolve(ok({ username: 'mmuster', firstName: 'Max', lastName: 'Muster' }));
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

beforeEach(async () => {
	mockPending([request]);
	await page.viewport(1280, 800);
});

test('denies access without the right to resolve role change requests', async () => {
	renderPage({ ...administratorAccess, resolveRoleChangeRequests: false });

	await expect.element(page.getByText('Kein Zugriff')).toBeInTheDocument();
	expect(api.GET).not.toHaveBeenCalledWith(pendingPath, expect.anything());
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
	api.GET.mockImplementation((path: string) =>
		Promise.resolve(path === pendingPath ? failed(500) : ok([]))
	);
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
	api.PUT.mockResolvedValue(failed(409, { id: 7, reason: 'user does not exist anymore' }));
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
