import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page as browserPage, userEvent } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import { goto } from '$app/navigation';
import apiClient from '#lib/api/client.js';
import { getAuth } from '#lib/auth/auth.svelte.js';
import { childrenText, failed, ok, resetApp } from '../../test-utils';
import AppLayout from './AppLayout.svelte';

// Keeps the user access store from calling the real API
vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn() } }));

// The layout is rendered outside of a SvelteKit app, so the current page is set by the tests
const appState = vi.hoisted(() => ({
	page: {
		url: new URL('http://localhost/'),
		route: { id: '/' as string | null },
		params: {} as Record<string, string>
	}
}));
vi.mock('$app/state', () => appState);

const get = apiClient.GET as Mock;

beforeEach(async () => {
	resetApp();
	get.mockResolvedValue(failed(401));
	appState.page.url = new URL('http://localhost/');
	appState.page.route.id = '/';
	appState.page.params = {};
	// Narrow, like a phone: the navigation is in a drawer
	await browserPage.viewport(400, 800);
});

function renderLayout() {
	return render(AppLayout, { children: childrenText('Seiteninhalt') });
}

async function openNavigation() {
	await userEvent.click(browserPage.getByRole('button', { name: 'Navigation öffnen' }));
	const nav = browserPage.getByRole('navigation', { name: 'Hauptnavigation' });
	await expect.element(nav).toBeInTheDocument();
	return nav;
}

test('opens the navigation drawer on mobile', async () => {
	renderLayout();
	await expect.element(browserPage.getByText('Seiteninhalt')).toBeInTheDocument();
	await expect.element(browserPage.getByRole('navigation', { name: 'Hauptnavigation' })).not.toBeInTheDocument();
	await openNavigation();
});

test('shows the navigation next to the page on wide screens', async () => {
	await browserPage.viewport(1280, 800);
	renderLayout();
	await expect.element(browserPage.getByRole('navigation', { name: 'Hauptnavigation' })).toBeInTheDocument();
	await expect.element(browserPage.getByRole('button', { name: 'Navigation öffnen' })).not.toBeInTheDocument();
});

test('shows the logged-in user and logs out', async () => {
	renderLayout();
	const nav = await openNavigation();

	await expect.element(nav.getByText('Max Muster')).toBeInTheDocument();
	// The cryptic user name is not shown next to the full name
	await expect.element(nav.getByText('mmuster')).not.toBeInTheDocument();

	await userEvent.click(nav.getByRole('button', { name: 'Abmelden' }));
	expect(getAuth().logout).toHaveBeenCalled();
});

test('shows at most two roles, administration roles first', async () => {
	get.mockResolvedValue(
		ok({
			administrationRoles: ['FinanceReviewer'],
			accountingUnits: [
				{ key: 'fsr-dit', name: 'FSR DIT', accountingUnitRoles: ['Treasurer'], fiscalYears: [] },
				{ key: 'fsr-im', name: 'FSR IM', accountingUnitRoles: ['FinanceOfficer'], fiscalYears: [] }
			]
		})
	);

	renderLayout();
	const nav = await openNavigation();

	await expect.element(nav.getByText('Finanzprüfung')).toBeInTheDocument();
	await expect.element(nav.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
	await expect.element(nav.getByText('FSR IM · Finanzer')).not.toBeInTheDocument();
});

test('offers only the accessible Haushalte and fiscal years, edit buttons only with manage rights', async () => {
	get.mockResolvedValue(
		ok({
			manageAccountingUnits: false,
			administrationRoles: [],
			accountingUnits: [
				{
					key: 'fsr-dit',
					name: 'FSR DIT',
					manageFiscalYears: true,
					accountingUnitRoles: ['FinanceOfficer'],
					fiscalYears: [
						{ key: 'dit-2026', name: '2026' },
						{ key: 'dit-2025', name: '2025' }
					]
				},
				{
					key: 'fsr-im',
					name: 'FSR IM',
					manageFiscalYears: false,
					accountingUnitRoles: ['Treasurer'],
					fiscalYears: [{ key: 'im-2024', name: '2024' }]
				}
			]
		})
	);

	renderLayout();
	const nav = await openNavigation();
	const accountingUnitSelect = nav.getByLabelText('Haushalt', { exact: true });
	const fiscalYearSelect = nav.getByLabelText('Haushaltsjahr', { exact: true });

	// The first Haushalt and its first fiscal year are preselected
	await expect.element(accountingUnitSelect).toHaveTextContent('FSR DIT');
	await expect.element(fiscalYearSelect).toHaveTextContent('2026');
	await expect.element(nav.getByRole('link', { name: 'Haushalt bearbeiten' })).not.toBeInTheDocument();
	await expect.element(nav.getByRole('button', { name: 'Haushaltsjahr bearbeiten' })).toBeInTheDocument();

	// Switching the Haushalt switches to its own fiscal years and rights
	await userEvent.click(accountingUnitSelect);
	await userEvent.click(browserPage.getByRole('option', { name: 'FSR IM' }));

	await expect.element(fiscalYearSelect).toHaveTextContent('2024');
	await expect.element(nav.getByRole('button', { name: 'Haushaltsjahr bearbeiten' })).not.toBeInTheDocument();
});

test('leaves the fiscal year page for a Haushalt whose fiscal years the user may not manage', async () => {
	get.mockResolvedValue(
		ok({
			administrationRoles: [],
			accountingUnits: [
				{ key: 'fsr-dit', name: 'FSR DIT', manageFiscalYears: true, accountingUnitRoles: [], fiscalYears: [] },
				{ key: 'fsr-im', name: 'FSR IM', manageFiscalYears: false, accountingUnitRoles: [], fiscalYears: [] }
			]
		})
	);
	appState.page.url = new URL('http://localhost/haushaltsjahre-verwalten/fsr-dit');
	appState.page.route.id = '/haushaltsjahre-verwalten/[accountingUnitKey]';
	appState.page.params = { accountingUnitKey: 'fsr-dit' };

	renderLayout();
	const nav = await openNavigation();
	await userEvent.click(nav.getByLabelText('Haushalt', { exact: true }));
	await userEvent.click(browserPage.getByRole('option', { name: 'FSR IM' }));

	expect(goto).toHaveBeenCalledWith('/');
});

test.each([
	{ resolveRoleChangeRequests: true, shown: true },
	{ resolveRoleChangeRequests: false, shown: false }
])(
	'shows the administration only to users who may resolve role change requests ($shown)',
	async ({ resolveRoleChangeRequests, shown }) => {
		get.mockResolvedValue(ok({ resolveRoleChangeRequests, administrationRoles: [], accountingUnits: [] }));

		renderLayout();
		const nav = await openNavigation();

		// Waits until the user access is loaded, the Haushalt select then no longer says it is loading
		await expect.element(nav.getByText('Kein Haushalt verfügbar')).toBeInTheDocument();
		const link = nav.getByRole('link', { name: 'Administration' });
		if (shown) await expect.element(link).toBeInTheDocument();
		else await expect.element(link).not.toBeInTheDocument();
	}
);
