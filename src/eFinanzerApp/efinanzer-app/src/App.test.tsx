import type { Mock } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import apiClient from './common/api/client';
import keycloak, { initKeycloak } from './common/auth/keycloak';
import App from './App';

// Keeps the user access store from calling the real API
vi.mock('./common/api/client', () => ({ default: { GET: vi.fn() } }));

// Pretends an existing Keycloak session instead of redirecting to the login page
vi.mock('./common/auth/keycloak', () => ({
  initKeycloak: vi.fn(),
  default: {
    tokenParsed: { preferred_username: 'mmuster', name: 'Max Muster' },
    logout: vi.fn(),
  },
}));

// Set per test, so every test starts with the same responses
beforeEach(() => {
  (apiClient.GET as Mock).mockResolvedValue({ data: undefined, response: { ok: false, status: 401 } });
  (initKeycloak as Mock).mockResolvedValue(true);
  (keycloak.logout as Mock).mockResolvedValue(undefined);
});

// The matchMedia mock in setupTests matches no breakpoint, so the mobile layout is rendered.
test('opens the navigation drawer on mobile', async () => {
  render(<App />);
  userEvent.click(await screen.findByRole('button', { name: 'Navigation öffnen' }));
  expect(await screen.findByRole('navigation', { name: 'Hauptnavigation' })).toBeInTheDocument();
});

test('shows the logged-in user and logs out', async () => {
  render(<App />);
  userEvent.click(await screen.findByRole('button', { name: 'Navigation öffnen' }));

  expect(await screen.findByText('Max Muster')).toBeInTheDocument();
  // The cryptic user name is not shown next to the full name
  expect(screen.queryByText('mmuster')).not.toBeInTheDocument();

  userEvent.click(screen.getByRole('button', { name: 'Abmelden' }));
  expect(keycloak.logout).toHaveBeenCalledWith({ redirectUri: `${window.location.origin}/` });
});

test('shows at most two roles, administration roles first', async () => {
  (apiClient.GET as Mock).mockResolvedValue({
    data: {
      administrationRoles: ['FinanceReviewer'],
      accountingUnits: [
        { key: 'fsr-dit', name: 'FSR DIT', accountingUnitRoles: ['Treasurer'], fiscalYears: [] },
        { key: 'fsr-im', name: 'FSR IM', accountingUnitRoles: ['FinanceOfficer'], fiscalYears: [] },
      ],
    },
    response: { ok: true, status: 200 },
  });

  render(<App />);
  userEvent.click(await screen.findByRole('button', { name: 'Navigation öffnen' }));

  expect(await screen.findByText('Finanzprüfung')).toBeInTheDocument();
  expect(screen.getByText('FSR DIT · Kassenwart')).toBeInTheDocument();
  expect(screen.queryByText('FSR IM · Finanzer')).not.toBeInTheDocument();
});

test('offers only the accessible Haushalte and fiscal years, edit buttons only with manage rights', async () => {
  (apiClient.GET as Mock).mockResolvedValue({
    data: {
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
            { key: 'dit-2025', name: '2025' },
          ],
        },
        {
          key: 'fsr-im',
          name: 'FSR IM',
          manageFiscalYears: false,
          accountingUnitRoles: ['Treasurer'],
          fiscalYears: [{ key: 'im-2024', name: '2024' }],
        },
      ],
    },
    response: { ok: true, status: 200 },
  });

  render(<App />);
  userEvent.click(await screen.findByRole('button', { name: 'Navigation öffnen' }));
  const nav = await screen.findByRole('navigation', { name: 'Hauptnavigation' });

  // The first Haushalt and its first fiscal year are preselected
  expect(await within(nav).findByTitle('FSR DIT')).toBeInTheDocument();
  expect(within(nav).getByTitle('2026')).toBeInTheDocument();
  expect(within(nav).queryByRole('button', { name: 'Haushalt bearbeiten' })).not.toBeInTheDocument();
  expect(within(nav).getByRole('button', { name: 'Haushaltsjahr bearbeiten' })).toBeInTheDocument();

  // Switching the Haushalt switches to its own fiscal years and rights
  fireEvent.mouseDown(within(nav).getByLabelText('Haushalt'));
  // The visible entry of the dropdown, antd's role="option" elements are only for screen readers
  const options = await screen.findAllByTitle('FSR IM');
  fireEvent.click(options.find((element) => element.classList.contains('ant-select-item-option'))!);

  expect(await within(nav).findByTitle('2024')).toBeInTheDocument();
  expect(within(nav).queryByRole('button', { name: 'Haushaltsjahr bearbeiten' })).not.toBeInTheDocument();
});

test.each([
  { resolveRoleChangeRequests: true, shown: true },
  { resolveRoleChangeRequests: false, shown: false },
])('shows the administration only to users who may resolve role change requests ($shown)', async ({ resolveRoleChangeRequests, shown }) => {
  (apiClient.GET as Mock).mockResolvedValue({
    data: { resolveRoleChangeRequests, administrationRoles: [], accountingUnits: [] },
    response: { ok: true, status: 200 },
  });

  render(<App />);
  userEvent.click(await screen.findByRole('button', { name: 'Navigation öffnen' }));
  const nav = await screen.findByRole('navigation', { name: 'Hauptnavigation' });

  // Waits until the user access is loaded, the Haushalt select is then no longer loading
  expect(await within(nav).findByText('Kein Haushalt verfügbar')).toBeInTheDocument();
  if (shown) expect(within(nav).getByRole('menuitem', { name: 'Administration' })).toBeInTheDocument();
  else expect(within(nav).queryByRole('menuitem', { name: 'Administration' })).not.toBeInTheDocument();
});
