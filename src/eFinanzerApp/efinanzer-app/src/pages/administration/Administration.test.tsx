import type { Mock } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from 'antd';
import { MemoryRouter, Route, Routes } from 'react-router';
import apiClient from '../../common/api/client';
import type { RoleChangeRequest, UserAccess } from '../../common/api/types';
import { AuthContext } from '../../common/auth/useAuth';
import { Provider } from 'react-redux';
import { makeStore } from '../../common/stores/store';
import RequireAdministrationRights from './RequireAdministrationRights';
import AdministrationPage from './AdministrationPage';

vi.mock('../../common/api/client', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const administratorAccess: UserAccess = {
  administrationRoles: ['FinanceAdministrator'],
  accountingUnits: [],
  resolveRoleChangeRequests: true,
};

const request: RoleChangeRequest = {
  id: 7,
  requestedBy: 'mmuster',
  createdAt: '2026-10-01T10:00:00Z',
  requestComment: 'Neu gewählt',
  roleChanges: [
    { accountingUnitKey: 'fsr-dit', accountingUnitRole: 'Treasurer', isAdded: true },
    { accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: false },
  ],
};

function ok<T>(data?: T, status = 200) {
  return { data, response: { ok: true, status } };
}

function failed(status: number, error: unknown = {}) {
  return { data: undefined, error, response: { ok: false, status } };
}

function mockPending(requests: RoleChangeRequest[], totalCount = requests.length) {
  api.GET.mockImplementation((path: string) => {
    if (path === '/api/manage-role-change-requests/list-pending-role-change-requests') {
      return Promise.resolve(ok({ items: requests, totalCount }));
    }
    if (path === '/api/manage-role-change-requests/list-accounting-units') {
      return Promise.resolve(ok([{ key: 'fsr-dit', name: 'FSR DIT' }, { key: 'fsr-im', name: 'FSR IM' }]));
    }
    if (path === '/api/get-users/get-user') {
      return Promise.resolve(ok({ username: 'mmuster', firstName: 'Max', lastName: 'Muster' }));
    }
    return Promise.resolve(failed(404));
  });
}

function renderPage(userAccess: UserAccess = administratorAccess) {
  return render(
    <Provider store={makeStore({ userAccess: { userAccess, status: 'loaded' } })}>
      <App>
        <AuthContext.Provider value={{ username: 'admin', displayName: 'Ada Admin', logout: vi.fn() }}>
          <MemoryRouter initialEntries={['/administration']}>
            <Routes>
              <Route path="administration" element={<RequireAdministrationRights />}>
                <Route index element={<AdministrationPage />} />
              </Route>
            </Routes>
          </MemoryRouter>
        </AuthContext.Provider>
      </App>
    </Provider>,
  );
}

beforeEach(() => {
  mockPending([request]);
});

test('denies access without the right to resolve role change requests', async () => {
  renderPage({ ...administratorAccess, resolveRoleChangeRequests: false });

  expect(await screen.findByText('Kein Zugriff')).toBeInTheDocument();
  expect(api.GET).not.toHaveBeenCalledWith(
    '/api/manage-role-change-requests/list-pending-role-change-requests',
    expect.anything(),
  );
});

test('shows the pending requests of the first page', async () => {
  renderPage();

  expect(await screen.findByText('Max Muster')).toBeInTheDocument();
  expect(screen.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
  expect(screen.getByText('FSR IM · Finanzer')).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/manage-role-change-requests/list-pending-role-change-requests', {
    params: { query: { PageSize: 20, Offset: 0 } },
  });
});

test('loads the next page', async () => {
  mockPending([request], 25);
  renderPage();

  userEvent.click(await screen.findByTitle('2'));

  await waitFor(() =>
    expect(api.GET).toHaveBeenCalledWith('/api/manage-role-change-requests/list-pending-role-change-requests', {
      params: { query: { PageSize: 20, Offset: 20 } },
    }),
  );
});

test('approves a request with a comment', async () => {
  api.PUT.mockResolvedValue(ok(undefined, 204));
  renderPage();

  const row = (await screen.findByText('Max Muster')).closest('tr')!;
  userEvent.click(within(row).getByRole('button', { name: /Annehmen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText(/Kommentar/), 'Willkommen');
  userEvent.click(within(dialog).getByRole('button', { name: 'Annehmen' }));

  await waitFor(() =>
    expect(api.PUT).toHaveBeenCalledWith('/api/manage-role-change-requests/resolve-role-change-request', {
      params: { query: { id: 7 } },
      body: { resolution: 'Approved', comment: 'Willkommen' },
    }),
  );
  expect(await screen.findByText('Antrag wurde angenommen')).toBeInTheDocument();
  // Only the roles of the requesting user changed
  expect(api.GET).not.toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('dismisses a request without a comment', async () => {
  api.PUT.mockResolvedValue(ok(undefined, 204));
  renderPage();

  const row = (await screen.findByText('Max Muster')).closest('tr')!;
  userEvent.click(within(row).getByRole('button', { name: /Ablehnen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.click(within(dialog).getByRole('button', { name: 'Ablehnen' }));

  await waitFor(() =>
    expect(api.PUT).toHaveBeenCalledWith('/api/manage-role-change-requests/resolve-role-change-request', {
      params: { query: { id: 7 } },
      body: { resolution: 'Dismissed', comment: null },
    }),
  );
  expect(await screen.findByText('Antrag wurde abgelehnt')).toBeInTheDocument();
});

test('keeps the dialog open if the request cannot be applied', async () => {
  api.PUT.mockResolvedValue(failed(409, { id: 7, reason: 'user does not exist anymore' }));
  renderPage();

  const row = (await screen.findByText('Max Muster')).closest('tr')!;
  userEvent.click(within(row).getByRole('button', { name: /Annehmen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.click(within(dialog).getByRole('button', { name: 'Annehmen' }));

  expect(await screen.findByText(/kann nicht angewendet werden/)).toBeInTheDocument();
  expect(screen.getByRole('dialog')).toBeInTheDocument();
});
