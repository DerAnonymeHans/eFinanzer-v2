import type { Mock } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from 'antd';
import { MemoryRouter, Route, Routes } from 'react-router';
import apiClient from '../../api/client';
import type { UserAccess } from '../../api/types';
import { UserAccessContext } from '../../stores/useUserAccess';
import { UserStoreProvider } from '../../stores/UserStore';
import RequireManageAccountingUnits from './RequireManageAccountingUnits';
import ManageAccountingUnitsPage from './ManageAccountingUnitsPage';
import AccountingUnitPage from './AccountingUnitPage';

vi.mock('../../api/client', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const reloadUserAccess = vi.fn();

const users: Record<string, { username: string; firstName: string; lastName: string }> = {
  mmuster: { username: 'mmuster', firstName: 'Max', lastName: 'Muster' },
  eexample: { username: 'eexample', firstName: 'Erika', lastName: 'Example' },
  nneu: { username: 'nneu', firstName: 'Nina', lastName: 'Neu' },
};
const managerAccess: UserAccess = { accountingGroups: [], administrationRoles: [], manageAccountingUnits: true };

function ok<T>(data?: T, status = 200) {
  return { data, response: { ok: true, status } };
}

function failed(status: number) {
  return { data: undefined, error: {}, response: { ok: false, status } };
}

function renderAt(path: string, userAccess: UserAccess = managerAccess) {
  return render(
    <UserStoreProvider>
      <App>
      <UserAccessContext.Provider value={{ userAccess, loading: false, error: undefined, reload: reloadUserAccess }}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="haushalte-verwalten" element={<RequireManageAccountingUnits />}>
              <Route index element={<ManageAccountingUnitsPage />} />
              <Route path=":key" element={<AccountingUnitPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </UserAccessContext.Provider>
      </App>
    </UserStoreProvider>,
  );
}

beforeEach(() => {
  reloadUserAccess.mockResolvedValue(undefined);
  api.GET.mockImplementation((path: string, options?: unknown) => {
    if (path === '/api/manage-accounting-units/list-accounting-units') {
      return Promise.resolve(
        ok([
          { key: 'fsr-dit', name: 'FSR DIT', description: 'Fachschaftsrat DIT' },
          { key: 'fsr-im', name: null, description: null },
        ]),
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
            { userName: 'eexample', roles: ['FinanceOfficer', 'FinanceAuthorizedUser'] },
          ],
        }),
      );
    }
    if (path === '/api/get-users/get-user') {
      const user = users[(options as { params: { query: { username: string } } }).params.query.username];
      return Promise.resolve(user ? ok(user) : failed(404));
    }
    if (path === '/api/get-users/search-user') {
      const search = (options as { params: { query: { search: string } } }).params.query.search.toLowerCase();
      return Promise.resolve(
        ok(Object.values(users).filter((user) => `${user.firstName} ${user.lastName}`.toLowerCase().includes(search))),
      );
    }
    return Promise.resolve(failed(404));
  });
});

/** Types into the user search and returns the visible result entries */
async function searchUser(text: string) {
  userEvent.type(screen.getByRole('combobox', { name: 'Benutzer suchen' }), text);
  await waitFor(() =>
    expect(api.GET).toHaveBeenCalledWith('/api/get-users/search-user', { params: { query: { search: text } } }),
  );
}

// The visible entry of the dropdown, antd's role="option" elements are only for screen readers
async function findResult(name: string) {
  const entries = await screen.findAllByTitle(name);
  return entries.find((element) => element.classList.contains('ant-select-item-option'))!;
}

test('denies access without the right to manage accounting units', async () => {
  renderAt('/haushalte-verwalten', { ...managerAccess, manageAccountingUnits: false });

  expect(await screen.findByText('Kein Zugriff')).toBeInTheDocument();
  expect(api.GET).not.toHaveBeenCalled();
});

test('shows one card per accounting unit', async () => {
  renderAt('/haushalte-verwalten');

  expect(await screen.findByText('FSR DIT')).toBeInTheDocument();
  expect(screen.getByText('Fachschaftsrat DIT')).toBeInTheDocument();
  // Without a name the key is the title
  expect(screen.getAllByText('fsr-im')).toHaveLength(2);
  expect(screen.getByText('Keine Beschreibung')).toBeInTheDocument();
  expect(screen.getAllByRole('button', { name: /bearbeiten$/ })).toHaveLength(2);
});

test('deletes an accounting unit only after confirming', async () => {
  api.DELETE.mockResolvedValue(ok(undefined, 204));
  renderAt('/haushalte-verwalten');

  userEvent.click(await screen.findByRole('button', { name: 'Haushalt FSR DIT löschen' }));
  const dialog = await screen.findByRole('dialog');
  expect(within(dialog).getAllByText('Haushalt „FSR DIT“ löschen?').length).toBeGreaterThan(0);
  expect(api.DELETE).not.toHaveBeenCalled();

  userEvent.click(within(dialog).getByRole('button', { name: 'Löschen' }));

  await waitFor(() =>
    expect(api.DELETE).toHaveBeenCalledWith('/api/manage-accounting-units/delete-accounting-unit', {
      params: { query: { key: 'fsr-dit' } },
    }),
  );
  expect(await screen.findByText('Haushalt „FSR DIT“ wurde gelöscht')).toBeInTheDocument();
  expect(reloadUserAccess).toHaveBeenCalled();
});

test('explains why an accounting unit with fiscal years cannot be deleted', async () => {
  api.DELETE.mockResolvedValue(failed(409));
  renderAt('/haushalte-verwalten');

  userEvent.click(await screen.findByRole('button', { name: 'Haushalt FSR DIT löschen' }));
  userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Löschen' }));

  expect(
    await screen.findByText('Der Haushalt hat noch Haushaltsjahre und kann daher nicht gelöscht werden'),
  ).toBeInTheDocument();
});

test('creates an accounting unit from the dialog', async () => {
  api.POST.mockResolvedValue(ok(undefined, 201));
  renderAt('/haushalte-verwalten');

  userEvent.click(await screen.findByRole('button', { name: /Haushalt anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), 'fsr-new');
  userEvent.type(within(dialog).getByLabelText(/^Name/), ' FSR Neu ');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  await waitFor(() =>
    expect(api.POST).toHaveBeenCalledWith('/api/manage-accounting-units/create-accounting-unit', {
      body: { key: 'fsr-new', name: 'FSR Neu', description: null },
    }),
  );
  expect(await screen.findByText('Haushalt wurde angelegt')).toBeInTheDocument();
  expect(reloadUserAccess).toHaveBeenCalled();
});

test('shows a taken key at the key input', async () => {
  api.POST.mockResolvedValue(failed(409));
  renderAt('/haushalte-verwalten');

  userEvent.click(await screen.findByRole('button', { name: /Haushalt anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), 'fsr-dit');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  expect(await within(dialog).findByText('Dieser Schlüssel ist bereits vergeben')).toBeInTheDocument();
});

test('does not send an invalid key', async () => {
  renderAt('/haushalte-verwalten');

  userEvent.click(await screen.findByRole('button', { name: /Haushalt anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), 'fsr dit');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  expect(await within(dialog).findByText('Nur Buchstaben a-z, Ziffern, "_" und "-"')).toBeInTheDocument();
  expect(api.POST).not.toHaveBeenCalled();
});

test('shows the general info and the users with their roles', async () => {
  renderAt('/haushalte-verwalten/fsr-dit');

  expect(await screen.findByRole('heading', { name: 'FSR DIT' })).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/manage-accounting-units/get-accounting-unit', {
    params: { query: { key: 'fsr-dit' } },
  });
  expect(screen.getByLabelText('Name')).toHaveValue('FSR DIT');
  expect(screen.getByLabelText('Beschreibung')).toHaveValue('Fachschaftsrat DIT');

  // Full names instead of the cryptic user names
  expect(await screen.findByText('Max Muster')).toBeInTheDocument();
  expect(await screen.findByText('Erika Example')).toBeInTheDocument();
  expect(screen.queryByText('mmuster')).not.toBeInTheDocument();
  expect(screen.getByText('Kassenwart')).toBeInTheDocument();
  expect(screen.getByText('Finanzer')).toBeInTheDocument();
  expect(screen.getByText('Finanzbevollmächtigter')).toBeInTheDocument();
});

test('saves the changed users only after confirming', async () => {
  api.PUT.mockResolvedValue(ok(undefined, 204));
  renderAt('/haushalte-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: 'Max Muster entfernen' }));
  await searchUser('Nina');
  fireEvent.click(await findResult('Nina Neu'));
  userEvent.click(screen.getByRole('button', { name: /Hinzufügen/ }));

  const rows = await screen.findAllByRole('button', { name: /entfernen$/ });
  expect(rows.map((row) => row.getAttribute('aria-label'))).toEqual(['Erika Example entfernen', 'Nina Neu entfernen']);
  expect(screen.queryByText('Max Muster')).not.toBeInTheDocument();
  // The search delivered the name, it is not loaded again
  expect(api.GET).not.toHaveBeenCalledWith('/api/get-users/get-user', { params: { query: { username: 'nneu' } } });

  userEvent.click(screen.getByRole('button', { name: /Speichern/ }));
  const dialog = await screen.findByRole('dialog');
  // The added user has no role yet
  expect(within(dialog).getByText(/aus dem Haushalt entfernt werden/)).toHaveTextContent('Nina Neu');
  expect(api.PUT).not.toHaveBeenCalled();

  userEvent.click(within(dialog).getByRole('button', { name: 'Speichern' }));

  await waitFor(() =>
    expect(api.PUT).toHaveBeenCalledWith('/api/manage-accounting-units/update-accounting-unit', {
      params: { query: { key: 'fsr-dit' } },
      body: {
        name: 'FSR DIT',
        description: 'Fachschaftsrat DIT',
        usersWithRoles: [
          { userName: 'eexample', roles: ['FinanceOfficer', 'FinanceAuthorizedUser'] },
          { userName: 'nneu', roles: [] },
        ],
      },
    }),
  );
  expect(await screen.findByText('Änderungen wurden gespeichert')).toBeInTheDocument();
  expect(reloadUserAccess).toHaveBeenCalled();
});

test('allows adding only after a search result was selected', async () => {
  renderAt('/haushalte-verwalten/fsr-dit');

  await screen.findByRole('button', { name: 'Max Muster entfernen' });
  const addButton = screen.getByRole('button', { name: /Hinzufügen/ });
  expect(addButton).toBeDisabled();

  await searchUser('Nina');
  expect(await findResult('Nina Neu')).toBeInTheDocument();
  // Typing alone selects nobody
  expect(addButton).toBeDisabled();
});

test('leaves users that are already listed out of the search results', async () => {
  renderAt('/haushalte-verwalten/fsr-dit');

  await screen.findByRole('button', { name: 'Max Muster entfernen' });
  await searchUser('Max');

  expect(await screen.findByText('Keine Benutzer gefunden')).toBeInTheDocument();
});
