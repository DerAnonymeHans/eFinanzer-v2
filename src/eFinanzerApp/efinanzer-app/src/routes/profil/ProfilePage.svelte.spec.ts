import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page, userEvent } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import apiClient from '#lib/api/client.js';
import type { RoleChangeRequest, UserAccess } from '#lib/api/types.js';
import { failed, ok, renderAppOverlays, resetApp } from '../../test-utils';
import ProfilePage from './+page.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const userAccess: UserAccess = {
	administrationRoles: [],
	accountingUnits: [
		{ key: 'fsr-dit', name: 'FSR DIT', manageFiscalYears: false, accountingUnitRoles: ['Treasurer'], fiscalYears: [] }
	]
};

const accountingUnits = [
	{ key: 'fsr-dit', name: 'FSR DIT' },
	{ key: 'fsr-im', name: 'FSR IM' }
];

const approvedRequest: RoleChangeRequest = {
	id: 1,
	requestedBy: 'mmuster',
	createdAt: '2026-09-01T10:00:00Z',
	requestComment: 'Neu gewählt',
	roleChanges: [{ accountingUnitKey: 'fsr-dit', accountingUnitRole: 'Treasurer', isAdded: true }],
	resolvedBy: 'admin',
	resolvedAt: '2026-09-02T10:00:00Z',
	resolution: 'Approved',
	resolutionComment: 'Passt'
};

const pendingRequest: RoleChangeRequest = {
	id: 2,
	requestedBy: 'mmuster',
	createdAt: '2026-10-01T10:00:00Z',
	roleChanges: [{ accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: true }]
};

function mockRequests(requests: RoleChangeRequest[]) {
	api.GET.mockImplementation((path: string) => {
		if (path === '/api/manage-role-change-requests/list-own-role-change-requests') return Promise.resolve(ok(requests));
		if (path === '/api/manage-role-change-requests/list-accounting-units') return Promise.resolve(ok(accountingUnits));
		if (path === '/api/get-users/get-user') {
			return Promise.resolve(ok({ username: 'admin', firstName: 'Ada', lastName: 'Admin' }));
		}
		return Promise.resolve(failed(404));
	});
}

beforeEach(async () => {
	for (const method of Object.values(api)) method.mockReset();
	resetApp(userAccess);
	// Wide enough for the button texts, on narrow screens only the icons are shown
	await page.viewport(1280, 800);
});

function renderPage() {
	renderAppOverlays();
	return render(ProfilePage);
}

test('shows the name and the current roles of the user', async () => {
	mockRequests([]);
	renderPage();

	await expect.element(page.getByRole('heading', { name: 'Max Muster' })).toBeInTheDocument();
	await expect.element(page.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
	await expect.element(page.getByText('Noch keine Rollenänderungsanträge gestellt')).toBeInTheDocument();
});

test('shows the pending request instead of the button for a new one', async () => {
	mockRequests([pendingRequest, approvedRequest]);
	renderPage();

	await expect.element(page.getByText('Offen', { exact: true })).toBeInTheDocument();
	await expect.element(page.getByText('FSR IM · Finanzer')).toBeInTheDocument();
	await expect.element(page.getByRole('button', { name: /Neuer Rollenänderungsantrag/ })).not.toBeInTheDocument();
});

test('shows previous requests with their resolution', async () => {
	mockRequests([approvedRequest]);
	renderPage();

	await expect.element(page.getByText('Frühere Anträge')).toBeInTheDocument();
	await expect.element(page.getByText('Neu gewählt')).toBeInTheDocument();
	await expect.element(page.getByText('Passt')).toBeInTheDocument();
	await expect.element(page.getByText('Ada Admin')).toBeInTheDocument();
});

test('creates a request with the roles to add and to remove', async () => {
	mockRequests([]);
	api.POST.mockResolvedValue(ok(undefined, 201));
	renderPage();

	await userEvent.click(page.getByRole('button', { name: /Neuer Rollenänderungsantrag/ }));
	const dialog = page.getByRole('dialog');
	await expect.element(dialog).toBeInTheDocument();

	// Roles the user already has can only be removed, the others only be requested
	await expect.element(dialog.getByRole('checkbox', { name: 'Beantragen: FSR DIT · Kassenwart' })).toBeDisabled();
	await expect.element(dialog.getByRole('checkbox', { name: 'Abgeben: FSR IM · Finanzer' })).toBeDisabled();

	await userEvent.click(dialog.getByRole('checkbox', { name: 'Beantragen: FSR IM · Finanzer' }));
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Abgeben: FSR DIT · Kassenwart' }));
	await userEvent.fill(dialog.getByLabelText(/Begründung/), 'Ämterwechsel');
	await userEvent.click(dialog.getByRole('button', { name: 'Antrag stellen' }));

	await expect
		.poll(() => api.POST.mock.calls[0])
		.toEqual([
			'/api/manage-role-change-requests/create-role-change-request',
			{
				body: {
					comment: 'Ämterwechsel',
					roleChanges: [
						{ accountingUnitKey: 'fsr-dit', accountingUnitRole: 'Treasurer', isAdded: false },
						{ accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: true }
					]
				}
			}
		]);
	await expect.element(page.getByText('Rollenänderungsantrag wurde gestellt')).toBeInTheDocument();
});

test('does not create a request without roles', async () => {
	mockRequests([]);
	renderPage();

	await userEvent.click(page.getByRole('button', { name: /Neuer Rollenänderungsantrag/ }));
	const dialog = page.getByRole('dialog');
	await userEvent.click(dialog.getByRole('button', { name: 'Antrag stellen' }));

	await expect.element(dialog.getByText('Mindestens eine Rolle auswählen')).toBeInTheDocument();
	expect(api.POST).not.toHaveBeenCalled();
});

test('withdraws the pending request', async () => {
	mockRequests([pendingRequest]);
	api.DELETE.mockResolvedValue(ok(undefined, 204));
	renderPage();

	await userEvent.click(page.getByRole('button', { name: /Zurückziehen/ }));
	const dialog = page.getByRole('alertdialog');
	await expect.element(dialog).toBeInTheDocument();
	await userEvent.click(dialog.getByRole('button', { name: 'Zurückziehen' }));

	await expect
		.poll(() => api.DELETE.mock.calls[0])
		.toEqual(['/api/manage-role-change-requests/delete-role-change-request', { params: { query: { id: 2 } } }]);
	await expect.element(page.getByText('Rollenänderungsantrag wurde zurückgezogen')).toBeInTheDocument();
});
