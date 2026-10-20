import type { Mock } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from 'antd';
import apiClient from '../../common/api/client';
import type { RoleChangeRequest, UserAccess } from '../../common/api/types';
import { AuthContext } from '../../common/auth/useAuth';
import { UserAccessContext } from '../../common/stores/useUserAccess';
import { UserStoreProvider } from '../../common/stores/UserStore';
import ProfilePage from './ProfilePage';

vi.mock('../../common/api/client', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const userAccess: UserAccess = {
  administrationRoles: [],
  accountingUnits: [
    { key: 'fsr-dit', name: 'FSR DIT', manageFiscalYears: false, accountingUnitRoles: ['Treasurer'], fiscalYears: [] },
  ],
};

const accountingUnits = [
  { key: 'fsr-dit', name: 'FSR DIT' },
  { key: 'fsr-im', name: 'FSR IM' },
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
  resolutionComment: 'Passt',
};

const pendingRequest: RoleChangeRequest = {
  id: 2,
  requestedBy: 'mmuster',
  createdAt: '2026-10-01T10:00:00Z',
  roleChanges: [{ accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: true }],
};

function ok<T>(data?: T, status = 200) {
  return { data, response: { ok: true, status } };
}

function failed(status: number, error: unknown = {}) {
  return { data: undefined, error, response: { ok: false, status } };
}

function mockRequests(requests: RoleChangeRequest[]) {
  api.GET.mockImplementation((path: string) => {
    if (path === '/api/manage-role-change-requests/list-own-role-change-requests') return Promise.resolve(ok(requests));
    if (path === '/api/manage-role-change-requests/list-accounting-units') return Promise.resolve(ok(accountingUnits));
    if (path === '/api/get-users/get-user') return Promise.resolve(ok({ username: 'admin', firstName: 'Ada', lastName: 'Admin' }));
    return Promise.resolve(failed(404));
  });
}

function renderPage() {
  return render(
    <UserStoreProvider>
      <App>
        <AuthContext.Provider value={{ username: 'mmuster', displayName: 'Max Muster', logout: vi.fn() }}>
          <UserAccessContext.Provider value={{ userAccess, loading: false, error: undefined, reload: vi.fn() }}>
            <ProfilePage />
          </UserAccessContext.Provider>
        </AuthContext.Provider>
      </App>
    </UserStoreProvider>,
  );
}

test('shows the name and the current roles of the user', async () => {
  mockRequests([]);
  renderPage();

  expect(screen.getByRole('heading', { name: 'Max Muster' })).toBeInTheDocument();
  expect(screen.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
  expect(await screen.findByText('Noch keine Rollenänderungsanträge gestellt')).toBeInTheDocument();
});

test('shows the pending request instead of the button for a new one', async () => {
  mockRequests([pendingRequest, approvedRequest]);
  renderPage();

  expect(await screen.findByText('Offen')).toBeInTheDocument();
  expect(screen.getByText('FSR IM · Finanzer')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: /Neuer Rollenänderungsantrag/ })).not.toBeInTheDocument();
});

test('shows previous requests with their resolution', async () => {
  mockRequests([approvedRequest]);
  renderPage();

  expect(await screen.findByText('Frühere Anträge')).toBeInTheDocument();
  expect(screen.getByText('Neu gewählt')).toBeInTheDocument();
  expect(screen.getByText('Passt')).toBeInTheDocument();
  expect(await screen.findByText('Ada Admin')).toBeInTheDocument();
});

test('creates a request with the roles to add and to remove', async () => {
  mockRequests([]);
  api.POST.mockResolvedValue(ok(undefined, 201));
  renderPage();

  userEvent.click(await screen.findByRole('button', { name: /Neuer Rollenänderungsantrag/ }));
  const dialog = await screen.findByRole('dialog');

  // Roles the user already has can only be removed, the others only be requested
  expect(within(dialog).getByRole('checkbox', { name: 'Beantragen: FSR DIT · Kassenwart' })).toBeDisabled();
  expect(within(dialog).getByRole('checkbox', { name: 'Abgeben: FSR IM · Finanzer' })).toBeDisabled();

  userEvent.click(within(dialog).getByRole('checkbox', { name: 'Beantragen: FSR IM · Finanzer' }));
  userEvent.click(within(dialog).getByRole('checkbox', { name: 'Abgeben: FSR DIT · Kassenwart' }));
  userEvent.type(within(dialog).getByLabelText(/Begründung/), 'Ämterwechsel');
  userEvent.click(within(dialog).getByRole('button', { name: 'Antrag stellen' }));

  await waitFor(() =>
    expect(api.POST).toHaveBeenCalledWith('/api/manage-role-change-requests/create-role-change-request', {
      body: {
        comment: 'Ämterwechsel',
        roleChanges: [
          { accountingUnitKey: 'fsr-dit', accountingUnitRole: 'Treasurer', isAdded: false },
          { accountingUnitKey: 'fsr-im', accountingUnitRole: 'FinanceOfficer', isAdded: true },
        ],
      },
    }),
  );
  expect(await screen.findByText('Rollenänderungsantrag wurde gestellt')).toBeInTheDocument();
});

test('does not create a request without roles', async () => {
  mockRequests([]);
  renderPage();

  userEvent.click(await screen.findByRole('button', { name: /Neuer Rollenänderungsantrag/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.click(within(dialog).getByRole('button', { name: 'Antrag stellen' }));

  expect(await within(dialog).findByText('Mindestens eine Rolle auswählen')).toBeInTheDocument();
  expect(api.POST).not.toHaveBeenCalled();
});

test('withdraws the pending request', async () => {
  mockRequests([pendingRequest]);
  api.DELETE.mockResolvedValue(ok(undefined, 204));
  renderPage();

  userEvent.click(await screen.findByRole('button', { name: /Zurückziehen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.click(within(dialog).getByRole('button', { name: 'Zurückziehen' }));

  await waitFor(() =>
    expect(api.DELETE).toHaveBeenCalledWith('/api/manage-role-change-requests/delete-role-change-request', {
      params: { query: { id: 2 } },
    }),
  );
});
