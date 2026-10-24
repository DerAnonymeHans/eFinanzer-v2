import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page, userEvent } from 'vitest/browser';
import type { ComponentProps } from 'svelte';
import { render } from 'vitest-browser-svelte';
import { toast } from 'svelte-sonner';
import apiClient from '#lib/api/client.js';
import type { UserAccess } from '#lib/api/types.js';
import { confirmState } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
import { childrenText, failed, ok, renderAppOverlays, resetApp } from '../../test-utils';
import Layout from './+layout.svelte';
import ListPage from './+page.svelte';
import DetailPage from './[accountingUnitKey]/+page.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const users: Record<string, { username: string; firstName: string; lastName: string }> = {
	mmuster: { username: 'mmuster', firstName: 'Max', lastName: 'Muster' },
	eexample: { username: 'eexample', firstName: 'Erika', lastName: 'Example' },
	nneu: { username: 'nneu', firstName: 'Nina', lastName: 'Neu' }
};
const managerAccess: UserAccess = { accountingUnits: [], administrationRoles: [], manageAccountingUnits: true };

beforeEach(async () => {
	resetApp(managerAccess);
	toast.dismiss();
	confirmState.open = false;
	await page.viewport(1280, 800);

	api.GET.mockImplementation((path: string, options?: unknown) => {
		if (path === '/api/manage-accounting-units/list-accounting-units') {
			return Promise.resolve(
				ok([
					{ key: 'fsr-dit', name: 'FSR DIT', description: 'Fachschaftsrat DIT' },
					{ key: 'fsr-im', name: null, description: null }
				])
			);
		}
		if (path === '/api/manage-accounting-units/get-accounting-unit') {
			return Promise.resolve(
				ok({
					key: 'fsr-dit',
					name: 'FSR DIT',
					description: 'Fachschaftsrat DIT',
					usersWithRoles: [
						{ userName: 'mmuster', roles: ['Treasurer'] },
						{ userName: 'eexample', roles: ['FinanceOfficer', 'FinanceAuthorizedUser'] }
					]
				})
			);
		}
		if (path === '/api/get-users/get-user') {
			const user = users[(options as { params: { query: { username: string } } }).params.query.username];
			return Promise.resolve(user ? ok(user) : failed(404));
		}
		if (path === '/api/get-users/search-user') {
			const search = (options as { params: { query: { search: string } } }).params.query.search.toLowerCase();
			return Promise.resolve(
				ok(Object.values(users).filter((user) => `${user.firstName} ${user.lastName}`.toLowerCase().includes(search)))
			);
		}
		if (path === '/api/get-user-access/my-user-access') return Promise.resolve(ok(managerAccess));
		return Promise.resolve(failed(404));
	});
});

function renderList() {
	renderAppOverlays();
	return render(ListPage);
}

function renderDetails() {
	renderAppOverlays();
	// Typed, so a renamed route parameter fails the type check. The cast leaves out the other page props.
	const props: Pick<ComponentProps<typeof DetailPage>, 'params'> = { params: { accountingUnitKey: 'fsr-dit' } };
	return render(DetailPage, props as never);
}

function userRow(fullName: string) {
	return page.getByRole('row').filter({ hasText: fullName });
}

/** Opens the user search of the dialog, types into it and waits for the search request */
async function searchUser(text: string) {
	// The trigger, the search input inside the opened popover has the same name
	await userEvent.click(page.getByRole('dialog').getByRole('combobox', { name: 'Benutzer suchen' }).first());
	await userEvent.fill(page.getByPlaceholder('Name, Benutzername oder E-Mail'), text);
	await expect
		.poll(() => api.GET.mock.calls)
		.toContainEqual(['/api/get-users/search-user', { params: { query: { search: text } } }]);
}

test('denies access without the right to manage accounting units', async () => {
	resetApp({ ...managerAccess, manageAccountingUnits: false });
	render(Layout, { children: childrenText('Seiteninhalt') } as never);

	await expect.element(page.getByText('Kein Zugriff')).toBeInTheDocument();
	await expect.element(page.getByText('Seiteninhalt')).not.toBeInTheDocument();
	expect(api.GET).not.toHaveBeenCalled();
});

test('shows the pages with the right to manage accounting units', async () => {
	render(Layout, { children: childrenText('Seiteninhalt') } as never);

	await expect.element(page.getByText('Seiteninhalt')).toBeInTheDocument();
	await expect.element(page.getByText('Kein Zugriff')).not.toBeInTheDocument();
});

test('shows one card per accounting unit', async () => {
	renderList();

	await expect.element(page.getByText('FSR DIT')).toBeInTheDocument();
	await expect.element(page.getByText('Fachschaftsrat DIT')).toBeInTheDocument();
	// Without a name the key is the title
	expect(page.getByText('fsr-im', { exact: true }).all()).toHaveLength(2);
	await expect.element(page.getByText('Keine Beschreibung')).toBeInTheDocument();
	expect(page.getByRole('link', { name: /bearbeiten$/ }).all()).toHaveLength(2);
	await expect
		.element(page.getByRole('link', { name: 'Haushalt FSR DIT bearbeiten' }))
		.toHaveAttribute('href', '/haushalte-verwalten/fsr-dit');
});

test('deletes an accounting unit only after confirming', async () => {
	api.DELETE.mockResolvedValue(ok(undefined, 204));
	renderList();

	await userEvent.click(page.getByRole('button', { name: 'Haushalt FSR DIT löschen' }));
	const dialog = page.getByRole('alertdialog');
	await expect.element(dialog.getByText('Haushalt „FSR DIT“ löschen?')).toBeInTheDocument();
	expect(api.DELETE).not.toHaveBeenCalled();

	await userEvent.click(dialog.getByRole('button', { name: 'Löschen' }));

	await expect
		.poll(() => api.DELETE)
		.toHaveBeenCalledWith('/api/manage-accounting-units/delete-accounting-unit', {
			params: { query: { key: 'fsr-dit' } }
		});
	await expect.element(page.getByText('Haushalt „FSR DIT“ wurde gelöscht')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('explains why an accounting unit with fiscal years cannot be deleted', async () => {
	api.DELETE.mockResolvedValue(failed(409));
	renderList();

	await userEvent.click(page.getByRole('button', { name: 'Haushalt FSR DIT löschen' }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Löschen' }));

	await expect
		.element(page.getByText('Der Haushalt hat noch Haushaltsjahre und kann daher nicht gelöscht werden'))
		.toBeInTheDocument();
});

test('creates an accounting unit from the dialog', async () => {
	api.POST.mockResolvedValue(ok(undefined, 201));
	renderList();

	await userEvent.click(page.getByRole('button', { name: /Haushalt anlegen/ }));
	const dialog = page.getByRole('dialog');
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), 'fsr-new');
	await userEvent.fill(dialog.getByLabelText(/^Name/), ' FSR Neu ');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect
		.poll(() => api.POST)
		.toHaveBeenCalledWith('/api/manage-accounting-units/create-accounting-unit', {
			body: { key: 'fsr-new', name: 'FSR Neu', description: null }
		});
	await expect.element(page.getByText('Haushalt wurde angelegt')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('shows a taken key at the key input', async () => {
	api.POST.mockResolvedValue(failed(409));
	renderList();

	await userEvent.click(page.getByRole('button', { name: /Haushalt anlegen/ }));
	const dialog = page.getByRole('dialog');
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), 'fsr-dit');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect.element(dialog.getByText('Dieser Schlüssel ist bereits vergeben')).toBeInTheDocument();
});

test('does not send an invalid key', async () => {
	renderList();

	await userEvent.click(page.getByRole('button', { name: /Haushalt anlegen/ }));
	const dialog = page.getByRole('dialog');
	await userEvent.fill(dialog.getByLabelText('Schlüssel'), 'fsr dit');
	await userEvent.click(dialog.getByRole('button', { name: 'Anlegen' }));

	await expect.element(dialog.getByText('Nur Buchstaben a-z, Ziffern, "_" und "-"')).toBeInTheDocument();
	expect(api.POST).not.toHaveBeenCalled();
});

test('shows the general info and the users with their roles', async () => {
	renderDetails();

	await expect.element(page.getByRole('heading', { name: 'FSR DIT' })).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/manage-accounting-units/get-accounting-unit', {
		params: { query: { key: 'fsr-dit' } }
	});
	await expect.element(page.getByLabelText('Name', { exact: true })).toHaveValue('FSR DIT');
	await expect.element(page.getByLabelText('Beschreibung', { exact: true })).toHaveValue('Fachschaftsrat DIT');

	// Full names instead of the cryptic user names
	await expect.element(userRow('Max Muster')).toHaveTextContent('Kassenwart');
	await expect.element(userRow('Erika Example')).toHaveTextContent(/Finanzer\s*Finanzbevollmächtigter/);
	await expect.element(page.getByText('mmuster')).not.toBeInTheDocument();
});

test('changes the roles of a user only when saving', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderDetails();

	await userEvent.click(page.getByRole('button', { name: 'Rollen von Max Muster bearbeiten' }));
	const dialog = page.getByRole('dialog');
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Finanzer', exact: true }));
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect.element(page.getByRole('dialog')).not.toBeInTheDocument();
	await expect.element(userRow('Max Muster')).toHaveTextContent(/Finanzer\s*Kassenwart/);
	expect(api.PUT).not.toHaveBeenCalled();

	await userEvent.click(page.getByRole('button', { name: /Speichern/ }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Speichern' }));

	await expect
		.poll(() => api.PUT.mock.calls.at(-1)?.[1].body.usersWithRoles)
		.toEqual([
			{ userName: 'mmuster', roles: ['FinanceOfficer', 'Treasurer'] },
			{ userName: 'eexample', roles: ['FinanceOfficer', 'FinanceAuthorizedUser'] }
		]);
});

test('saves the changed users only after confirming', async () => {
	api.PUT.mockResolvedValue(ok(undefined, 204));
	renderDetails();

	// Removing needs no confirmation, it is only saved with the global button
	await userEvent.click(page.getByRole('button', { name: 'Max Muster entfernen' }));
	await expect.element(page.getByRole('alertdialog')).not.toBeInTheDocument();
	await expect.element(userRow('Max Muster')).not.toBeInTheDocument();

	await userEvent.click(page.getByRole('button', { name: 'Mitglied hinzufügen' }));
	const dialog = page.getByRole('dialog');
	await searchUser('Nina');
	await userEvent.click(page.getByRole('option', { name: 'Nina Neu' }));
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Kassenwart' }));
	await userEvent.click(dialog.getByRole('button', { name: 'Hinzufügen' }));
	await expect.element(userRow('Nina Neu')).toHaveTextContent('Kassenwart');
	// The search delivered the name, it is not loaded again
	expect(api.GET).not.toHaveBeenCalledWith('/api/get-users/get-user', { params: { query: { username: 'nneu' } } });

	await userEvent.click(page.getByRole('button', { name: /Speichern/ }));
	const confirmDialog = page.getByRole('alertdialog');
	await expect.element(confirmDialog.getByText(/Aus dem Haushalt entfernt werden/)).toHaveTextContent('Max Muster');
	expect(api.PUT).not.toHaveBeenCalled();

	await userEvent.click(confirmDialog.getByRole('button', { name: 'Speichern' }));

	await expect
		.poll(() => api.PUT)
		.toHaveBeenCalledWith('/api/manage-accounting-units/update-accounting-unit', {
			params: { query: { key: 'fsr-dit' } },
			body: {
				name: 'FSR DIT',
				description: 'Fachschaftsrat DIT',
				usersWithRoles: [
					{ userName: 'eexample', roles: ['FinanceOfficer', 'FinanceAuthorizedUser'] },
					{ userName: 'nneu', roles: ['Treasurer'] }
				]
			}
		});
	await expect.element(page.getByText('Änderungen wurden gespeichert')).toBeInTheDocument();
	expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

async function saveWithoutMaxMuster() {
	await userEvent.click(page.getByRole('button', { name: 'Max Muster entfernen' }));
	await userEvent.click(page.getByRole('button', { name: /Speichern/ }));
	await userEvent.click(page.getByRole('alertdialog').getByRole('button', { name: 'Speichern' }));
}

function accountingUnitLoads() {
	return api.GET.mock.calls.filter(([path]) => path === '/api/manage-accounting-units/get-accounting-unit').length;
}

test('names the role that could not be changed and reloads if the changes were saved only partly', async () => {
	api.PUT.mockResolvedValue(failed(500, { type: 'ChangeUserRoleError', userName: 'mmuster', role: 'Treasurer', added: false }));
	renderDetails();
	await expect.element(userRow('Max Muster')).toBeInTheDocument();
	const loadsBefore = accountingUnitLoads();

	await saveWithoutMaxMuster();

	await expect
		.element(
			page.getByText(
				'Änderungen konnten nicht vollständig gespeichert werden: Die Rolle Kassenwart von Max Muster konnte nicht entfernt werden'
			)
		)
		.toBeInTheDocument();
	await expect.poll(accountingUnitLoads).toBeGreaterThan(loadsBefore);
});

test('names a user that does not exist anymore', async () => {
	api.PUT.mockResolvedValue(failed(400, { type: 'UnknownUserError', userName: 'eexample' }));
	renderDetails();
	await expect.element(userRow('Max Muster')).toBeInTheDocument();

	await saveWithoutMaxMuster();

	await expect.element(page.getByText('Den Benutzer Erika Example gibt es nicht mehr')).toBeInTheDocument();
});

test('reports a failure without partial changes', async () => {
	api.PUT.mockResolvedValue(failed(500));
	renderDetails();
	await expect.element(userRow('Max Muster')).toBeInTheDocument();

	await saveWithoutMaxMuster();

	await expect.element(page.getByText('Änderungen konnten nicht gespeichert werden (HTTP 500)')).toBeInTheDocument();
});

test('removes a user whose roles were all taken away', async () => {
	renderDetails();

	await userEvent.click(page.getByRole('button', { name: 'Rollen von Max Muster bearbeiten' }));
	const dialog = page.getByRole('dialog');
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Kassenwart' }));
	await expect.element(dialog.getByText('Ohne Rolle wird der Benutzer entfernt.')).toBeInTheDocument();
	await userEvent.click(dialog.getByRole('button', { name: 'Speichern' }));

	await expect.element(userRow('Max Muster')).not.toBeInTheDocument();
	expect(api.PUT).not.toHaveBeenCalled();
});

test('allows adding only after a search result was selected', async () => {
	renderDetails();

	await userEvent.click(page.getByRole('button', { name: 'Mitglied hinzufügen' }));
	const dialog = page.getByRole('dialog');
	await userEvent.click(dialog.getByRole('checkbox', { name: 'Kassenwart' }));
	const addButton = dialog.getByRole('button', { name: 'Hinzufügen' });
	await expect.element(addButton).toBeDisabled();

	await searchUser('Nina');
	await expect.element(page.getByRole('option', { name: 'Nina Neu' })).toBeInTheDocument();
	// Typing alone selects nobody
	await expect.element(addButton).toBeDisabled();
});

test('leaves users that are already listed out of the search results', async () => {
	renderDetails();

	await userEvent.click(page.getByRole('button', { name: 'Mitglied hinzufügen' }));
	await searchUser('Max');

	await expect.element(page.getByText('Keine Benutzer gefunden')).toBeInTheDocument();
});
