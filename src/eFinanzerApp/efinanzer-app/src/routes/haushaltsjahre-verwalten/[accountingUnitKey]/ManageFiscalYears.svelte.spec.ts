import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page, userEvent } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import apiClient from '#lib/api/client.js';
import type { FiscalYearListItem, UserAccess } from '#lib/api/types.js';
import { childrenText, failed, ok, renderAppOverlays, resetApp } from '../../../test-utils';
import Layout from './+layout.svelte';
import PageTestHarness from './PageTestHarness.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const officerAccess: UserAccess = {
	administrationRoles: [],
	accountingUnits: [
		{ key: 'fsr-dit', name: 'FSR DIT', manageFiscalYears: true, accountingUnitRoles: ['FinanceOfficer'], fiscalYears: [] },
		{ key: 'fsr-im', name: 'FSR IM', manageFiscalYears: false, accountingUnitRoles: ['Treasurer'], fiscalYears: [] }
	]
};

const fiscalYears: FiscalYearListItem[] = [
	{
		key: '2026',
		accountingUnitKey: 'fsr-dit',
		name: 'HHJ 26',
		startDate: '2026-01-01',
		endDate: '2026-12-31',
		receiptsCount: 0,
		budgetsCount: 0
	},
	{
		key: '2025',
		accountingUnitKey: 'fsr-dit',
		name: null,
		startDate: '2025-01-01',
		endDate: '2025-12-31',
		receiptsCount: 12,
		budgetsCount: 3
	}
];

beforeEach(async () => {
	for (const mock of Object.values(api)) mock.mockReset();
	resetApp(officerAccess);
	api.GET.mockImplementation((path: string) =>
		Promise.resolve(path === '/api/manage-fiscal-years/list-fiscal-years' ? ok(fiscalYears) : failed(404))
	);
	await page.viewport(1280, 800);
});

function renderPage(accountingUnitKey = 'fsr-dit') {
	renderAppOverlays();
	render(PageTestHarness, { accountingUnitKey });
}

async function openCreateDialog() {
	await userEvent.click(page.getByRole('button', { name: 'Haushaltsjahr anlegen' }));
	const dialog = page.getByRole('dialog');
	await expect.element(dialog).toBeInTheDocument();
	return dialog;
}

async function openEditDialog(name: string) {
	await userEvent.click(page.getByRole('button', { name: `Haushaltsjahr ${name} bearbeiten` }));
	const dialog = page.getByRole('dialog');
	await expect.element(dialog).toBeInTheDocument();
	return dialog;
}

test('denies access without the right to manage the fiscal years of the Haushalt', async () => {
	render(Layout, { params: { accountingUnitKey: 'fsr-im' }, children: childrenText('Seiteninhalt') } as never);

	await expect.element(page.getByText('Kein Zugriff')).toBeInTheDocument();
	await expect.element(page.getByText('Seiteninhalt')).not.toBeInTheDocument();
	expect(api.GET).not.toHaveBeenCalled();
});

test('shows the page with the right to manage the fiscal years of the Haushalt', async () => {
	render(Layout, { params: { accountingUnitKey: 'fsr-dit' }, children: childrenText('Seiteninhalt') } as never);

	await expect.element(page.getByText('Seiteninhalt')).toBeInTheDocument();
	await expect.element(page.getByText('Kein Zugriff')).not.toBeInTheDocument();
});

test('lists the fiscal years of the Haushalt', async () => {
	renderPage();

	await expect.element(page.getByText('HHJ 26')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/manage-fiscal-years/list-fiscal-years', {
		params: { query: { accountingUnitKey: 'fsr-dit' } }
	});
	await expect.element(page.getByText('Haushalt FSR DIT')).toBeInTheDocument();
	await expect.element(page.getByText('01.01.2026 – 31.12.2026')).toBeInTheDocument();
	// Without a name the key is shown instead
	await expect.element(page.getByText('01.01.2025 – 31.12.2025')).toBeInTheDocument();
	expect(page.getByText('2025', { exact: true }).all()).toHaveLength(1);
	await expect.element(page.getByText('12', { exact: true })).toBeInTheDocument();
});

test('allows deleting only fiscal years without budgets and receipts', async () => {
	renderPage();

	const deletable = page.getByRole('button', { name: 'Haushaltsjahr HHJ 26 löschen' });
	const notDeletable = page.getByRole('button', { name: 'Haushaltsjahr 2025 löschen' });
	await expect.element(deletable).not.toHaveAttribute('aria-disabled');
	await expect.element(notDeletable).toHaveAttribute('aria-disabled', 'true');

	// The reason is shown in a tooltip, the click does nothing
	await userEvent.hover(notDeletable);
	await expect
		.element(page.getByText('Hat noch Budgets oder Belege und kann nicht gelöscht werden').first())
		.toBeInTheDocument();
	// Forced, Playwright does not click controls marked as disabled on its own
	await userEvent.click(notDeletable, { force: true });
	await expect.element(page.getByRole('alertdialog')).not.toBeInTheDocument();
});

test('deletes a fiscal year only after confirming', async () => {
	api.DELETE.mockResolvedValue(ok(undefined, 204));
	renderPage();

	await userEvent.click(page.getByRole('button', { name: 'Haushaltsjahr HHJ 26 löschen' }));
	const dialog = page.getByRole('alertdialog');
	await expect.element(dialog.getByText('Haushaltsjahr „HHJ 26“ löschen?')).toBeInTheDocument();
	expect(api.DELETE).not.toHaveBeenCalled();

	await userEvent.click(dialog.getByRole('button', { name: 'Löschen' }));

	await expect
		.poll(() => api.DELETE)
		.toHaveBeenCalledWith('/api/manage-fiscal-years/delete-fiscal-year', {
			params: { query: { accountingUnitKey: 'fsr-dit', key: '2026' } }
		});
	await expect.element(page.getByText('Haushaltsjahr „HHJ 26“ wurde gelöscht')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('creates a fiscal year following the latest one', async () => {
	api.POST.mockResolvedValue(ok(undefined, 201));
	renderPage();
	await expect.element(page.getByText('HHJ 26')).toBeInTheDocument();

	const dialog = await openCreateDialog();
	// The period is prefilled with the year after the latest fiscal year
	await expect.element(dialog.getByLabelText('Zeitraum')).toHaveTextContent('01.01.2027 – 31.12.2027');

	await userEvent.fill(dialog.getByLabelText('Schlüssel'), '2027');
	await userEvent.fill(dialog.getByLabelText(/^Name/), ' HHJ 27 ');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect
		.poll(() => api.POST)
		.toHaveBeenCalledWith('/api/manage-fiscal-years/create-fiscal-year', {
			body: { key: '2027', accountingUnitKey: 'fsr-dit', name: 'HHJ 27', startDate: '2027-01-01', endDate: '2027-12-31' }
		});
	await expect.element(page.getByText('Haushaltsjahr wurde angelegt')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('picks the period of a new fiscal year in the calendar', async () => {
	api.GET.mockResolvedValue(ok([]));
	api.POST.mockResolvedValue(ok(undefined, 201));
	renderPage();
	await expect.element(page.getByText('Noch keine Haushaltsjahre angelegt')).toBeInTheDocument();

	const dialog = await openCreateDialog();
	// Without an earlier fiscal year nothing is suggested
	await expect.element(dialog.getByLabelText('Zeitraum')).toHaveTextContent('Beginn – Ende wählen');
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), 'kurz');

	// The calendar shows the current month and the next one, so the period is shorter than allowed
	await userEvent.click(dialog.getByLabelText('Zeitraum'));
	const days = page.getByRole('button').filter({ hasText: /^(1|20)$/ });
	await expect.poll(() => days.all().length).toBeGreaterThanOrEqual(2);
	const [first, , twentieth] = days.all();
	await userEvent.click(first);
	await userEvent.click(twentieth ?? days.all()[1]);
	await expect.element(dialog.getByLabelText('Zeitraum')).not.toHaveTextContent('…');

	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));
	await expect.element(dialog.getByText('Das Haushaltsjahr muss mindestens 100 Tage lang sein')).toBeInTheDocument();
	expect(api.POST).not.toHaveBeenCalled();
});

test('shows a taken key at the key input', async () => {
	api.POST.mockResolvedValue(failed(409, { key: '2026', accountingUnitKey: 'fsr-dit' }));
	renderPage();
	await expect.element(page.getByText('HHJ 26')).toBeInTheDocument();

	const dialog = await openCreateDialog();
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), '2026');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect.element(dialog.getByText('Dieser Schlüssel ist bereits vergeben')).toBeInTheDocument();
});

test('names the fiscal year the new period overlaps', async () => {
	api.POST.mockResolvedValue(
		failed(409, {
			key: '2027',
			accountingUnitKey: 'fsr-dit',
			startDate: '2027-01-01',
			endDate: '2027-12-31',
			overlappingKey: '2027a',
			overlappingStartDate: '2027-06-01',
			overlappingEndDate: '2028-05-31'
		})
	);
	renderPage();
	await expect.element(page.getByText('HHJ 26')).toBeInTheDocument();

	const dialog = await openCreateDialog();
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), '2027');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect
		.element(dialog.getByText('Überschneidet sich mit dem Haushaltsjahr „2027a“ (01.06.2027 – 31.05.2028)'))
		.toBeInTheDocument();
});

test('does not send an invalid key', async () => {
	renderPage();
	await expect.element(page.getByText('HHJ 26')).toBeInTheDocument();

	const dialog = await openCreateDialog();
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), '20 27');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect.element(dialog.getByText('Nur Buchstaben a-z, Ziffern, "_" und "-"')).toBeInTheDocument();
	expect(api.POST).not.toHaveBeenCalled();
});

test('updates the name of a fiscal year', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderPage();

	const dialog = await openEditDialog('2025');
	await expect.element(dialog.getByLabelText('Schlüssel')).toBeDisabled();
	await expect.element(dialog.getByLabelText('Zeitraum')).toHaveTextContent('01.01.2025 – 31.12.2025');
	await expect.element(dialog.getByText('Alle Belege des Haushaltsjahres müssen im Zeitraum liegen')).toBeInTheDocument();

	await userEvent.fill(dialog.getByLabelText(/^Name/), 'HHJ 25');
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect
		.poll(() => api.PUT)
		.toHaveBeenCalledWith('/api/manage-fiscal-years/update-fiscal-year', {
			params: { query: { accountingUnitKey: 'fsr-dit', key: '2025' } },
			body: { name: 'HHJ 25', startDate: '2025-01-01', endDate: '2025-12-31' }
		});
	await expect.element(page.getByText('Haushaltsjahr wurde gespeichert')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('explains that receipts lie outside the changed period', async () => {
	api.PUT.mockResolvedValue(
		failed(409, { key: '2025', accountingUnitKey: 'fsr-dit', startDate: '2025-01-01', endDate: '2025-12-31' })
	);
	renderPage();

	const dialog = await openEditDialog('2025');
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect.element(dialog.getByText('Es gibt Belege außerhalb dieses Zeitraums')).toBeInTheDocument();
});

test('maps an invalid field reported by the API to its input', async () => {
	api.PUT.mockResolvedValue(failed(400, { field: 'Name', message: 'invalid' }));
	renderPage();

	const dialog = await openEditDialog('2025');
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect.element(dialog.getByText('Ungültige Eingabe')).toBeInTheDocument();
});
