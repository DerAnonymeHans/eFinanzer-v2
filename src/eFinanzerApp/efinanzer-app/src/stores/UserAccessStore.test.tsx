import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import apiClient from '../api/client';
import type { UserAccess } from '../api/types';
import { UserAccessProvider, useUserAccess } from './UserAccessStore';

jest.mock('../api/client', () => ({ __esModule: true, default: { GET: jest.fn() } }));
const get = apiClient.GET as jest.Mock;

const userAccess: UserAccess = {
  accountingGroups: [
    {
      key: 'fsr-dit',
      name: 'FSR DIT',
      accountingUnitRoles: ['Treasurer'],
      fiscalYears: [{ key: '2026', name: '2026' }],
    },
  ],
  administrationRoles: [],
};

function Consumer() {
  const { userAccess, loading, error, reload } = useUserAccess();
  return (
    <>
      <span>{loading ? 'loading' : 'idle'}</span>
      <span>{error?.message ?? 'no error'}</span>
      <span>{userAccess?.accountingGroups.map((group) => group.name).join(', ') ?? 'no access'}</span>
      <button onClick={() => reload()}>reload</button>
    </>
  );
}

beforeEach(() => get.mockReset());

test('loads the user access on mount', async () => {
  get.mockResolvedValue({ data: userAccess, response: { ok: true, status: 200 } });

  render(
    <UserAccessProvider>
      <Consumer />
    </UserAccessProvider>,
  );

  expect(screen.getByText('loading')).toBeInTheDocument();
  expect(await screen.findByText('FSR DIT')).toBeInTheDocument();
  expect(screen.getByText('idle')).toBeInTheDocument();
  expect(get).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('exposes an error when loading fails and recovers on reload', async () => {
  get.mockResolvedValueOnce({ data: undefined, response: { ok: false, status: 401 } });

  render(
    <UserAccessProvider>
      <Consumer />
    </UserAccessProvider>,
  );

  expect(
    await screen.findByText('Benutzerrechte konnten nicht geladen werden (HTTP 401)'),
  ).toBeInTheDocument();
  expect(screen.getByText('no access')).toBeInTheDocument();

  get.mockResolvedValueOnce({ data: userAccess, response: { ok: true, status: 200 } });
  userEvent.click(screen.getByRole('button', { name: 'reload' }));

  expect(await screen.findByText('FSR DIT')).toBeInTheDocument();
  await waitFor(() => expect(screen.getByText('no error')).toBeInTheDocument());
});

test('throws when used outside the provider', () => {
  jest.spyOn(console, 'error').mockImplementation(() => {});
  expect(() => render(<Consumer />)).toThrow('useUserAccess must be used inside a UserAccessProvider');
  (console.error as jest.Mock).mockRestore();
});
