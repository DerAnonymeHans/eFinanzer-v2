import type { Mock } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from 'antd';
import { MemoryRouter, Route, Routes } from 'react-router';
import apiClient from '../../common/api/client';
import type { FiscalYearListItem, UserAccess } from '../../common/api/types';
import { Provider } from 'react-redux';
import { makeStore } from '../../common/stores/store';
import RequireManageFiscalYears from './RequireManageFiscalYears';
import ManageFiscalYearsPage from './ManageFiscalYearsPage';

vi.mock('../../common/api/client', () => ({ default: { GET: vi.fn(), POST: vi.fn(), PUT: vi.fn(), DELETE: vi.fn() } }));
const api = apiClient as unknown as Record<'GET' | 'POST' | 'PUT' | 'DELETE', Mock>;

const officerAccess: UserAccess = {
  administrationRoles: [],
  accountingUnits: [
    { key: 'fsr-dit', name: 'FSR DIT', manageFiscalYears: true, accountingUnitRoles: ['FinanceOfficer'], fiscalYears: [] },
    { key: 'fsr-im', name: 'FSR IM', manageFiscalYears: false, accountingUnitRoles: ['Treasurer'], fiscalYears: [] },
  ],
};

const fiscalYears: FiscalYearListItem[] = [
  {
    key: '2026',
    accountingUnitKey: 'fsr-dit',
    name: 'HHJ 26',
    startDate: '2026-01-01',
    endDate: '2026-12-31',
    receiptsCount: 0,
    budgetsCount: 0,
  },
  {
    key: '2025',
    accountingUnitKey: 'fsr-dit',
    name: null,
    startDate: '2025-01-01',
    endDate: '2025-12-31',
    receiptsCount: 12,
    budgetsCount: 3,
  },
];

function ok<T>(data?: T, status = 200) {
  return { data, response: { ok: true, status } };
}

function failed(status: number, error: unknown = {}) {
  return { data: undefined, error, response: { ok: false, status } };
}

function renderAt(path: string, userAccess: UserAccess = officerAccess) {
  return render(
    <Provider store={makeStore({ userAccess: { userAccess, status: 'loaded' } })}>
      <App>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="haushaltsjahre-verwalten/:accountingUnitKey" element={<RequireManageFiscalYears />}>
              <Route index element={<ManageFiscalYearsPage />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </App>
    </Provider>,
  );
}

beforeEach(() => {
  api.GET.mockImplementation((path: string) =>
    Promise.resolve(path === '/api/manage-fiscal-years/list-fiscal-years' ? ok(fiscalYears) : failed(404)),
  );
});

test('denies access without the right to manage the fiscal years of the Haushalt', async () => {
  renderAt('/haushaltsjahre-verwalten/fsr-im');

  expect(await screen.findByText('Kein Zugriff')).toBeInTheDocument();
  expect(api.GET).not.toHaveBeenCalled();
});

test('lists the fiscal years of the Haushalt', async () => {
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  expect(await screen.findByText('HHJ 26')).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/manage-fiscal-years/list-fiscal-years', {
    params: { query: { accountingUnitKey: 'fsr-dit' } },
  });
  expect(screen.getByText('Haushalt FSR DIT')).toBeInTheDocument();
  expect(screen.getByText('01.01.2026 – 31.12.2026')).toBeInTheDocument();
  // Without a name the key is shown instead
  expect(screen.getByText('01.01.2025 – 31.12.2025')).toBeInTheDocument();
  expect(screen.getAllByText('2025')).toHaveLength(1);
  expect(screen.getByText('12')).toBeInTheDocument();
});

test('allows deleting only fiscal years without accountingUnits and receipts', async () => {
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  expect(await screen.findByRole('button', { name: 'Haushaltsjahr HHJ 26 löschen' })).toBeEnabled();
  expect(screen.getByRole('button', { name: 'Haushaltsjahr 2025 löschen' })).toBeDisabled();
});

test('deletes a fiscal year only after confirming', async () => {
  api.DELETE.mockResolvedValue(ok(undefined, 204));
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: 'Haushaltsjahr HHJ 26 löschen' }));
  const dialog = await screen.findByRole('dialog');
  expect(within(dialog).getAllByText('Haushaltsjahr „HHJ 26“ löschen?').length).toBeGreaterThan(0);
  expect(api.DELETE).not.toHaveBeenCalled();

  userEvent.click(within(dialog).getByRole('button', { name: 'Löschen' }));

  await waitFor(() =>
    expect(api.DELETE).toHaveBeenCalledWith('/api/manage-fiscal-years/delete-fiscal-year', {
      params: { query: { accountingUnitKey: 'fsr-dit', key: '2026' } },
    }),
  );
  expect(await screen.findByText('Haushaltsjahr „HHJ 26“ wurde gelöscht')).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('creates a fiscal year following the latest one', async () => {
  api.POST.mockResolvedValue(ok(undefined, 201));
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: /Haushaltsjahr anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  // The period is prefilled with the year after the latest fiscal year
  expect(within(dialog).getByDisplayValue('01.01.2027')).toBeInTheDocument();
  expect(within(dialog).getByDisplayValue('31.12.2027')).toBeInTheDocument();

  userEvent.type(within(dialog).getByLabelText('Schlüssel'), '2027');
  userEvent.type(within(dialog).getByLabelText(/^Name/), ' HHJ 27 ');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  await waitFor(() =>
    expect(api.POST).toHaveBeenCalledWith('/api/manage-fiscal-years/create-fiscal-year', {
      body: { key: '2027', accountingUnitKey: 'fsr-dit', name: 'HHJ 27', startDate: '2027-01-01', endDate: '2027-12-31' },
    }),
  );
  expect(await screen.findByText('Haushaltsjahr wurde angelegt')).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('shows a taken key at the key input', async () => {
  api.POST.mockResolvedValue(failed(409, { key: '2026', accountingUnitKey: 'fsr-dit' }));
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: /Haushaltsjahr anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), '2026');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  expect(await within(dialog).findByText('Dieser Schlüssel ist bereits vergeben')).toBeInTheDocument();
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
      overlappingEndDate: '2028-05-31',
    }),
  );
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: /Haushaltsjahr anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), '2027');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  expect(
    await within(dialog).findByText('Überschneidet sich mit dem Haushaltsjahr „2027a“ (01.06.2027 – 31.05.2028)'),
  ).toBeInTheDocument();
});

test('does not send an invalid key', async () => {
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: /Haushaltsjahr anlegen/ }));
  const dialog = await screen.findByRole('dialog');
  userEvent.type(within(dialog).getByLabelText('Schlüssel'), '20 27');
  userEvent.click(within(dialog).getByRole('button', { name: 'Anlegen' }));

  expect(await within(dialog).findByText('Nur Buchstaben a-z, Ziffern, "_" und "-"')).toBeInTheDocument();
  expect(api.POST).not.toHaveBeenCalled();
});

test('updates the name of a fiscal year', async () => {
  api.PUT.mockResolvedValue(ok(undefined, 204));
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: 'Haushaltsjahr 2025 bearbeiten' }));
  const dialog = await screen.findByRole('dialog');
  expect(within(dialog).getByLabelText('Schlüssel')).toBeDisabled();
  expect(within(dialog).getByDisplayValue('01.01.2025')).toBeInTheDocument();
  expect(within(dialog).getByText('Alle Belege des Haushaltsjahres müssen im Zeitraum liegen')).toBeInTheDocument();

  userEvent.type(within(dialog).getByLabelText(/^Name/), 'HHJ 25');
  userEvent.click(within(dialog).getByRole('button', { name: 'Speichern' }));

  await waitFor(() =>
    expect(api.PUT).toHaveBeenCalledWith('/api/manage-fiscal-years/update-fiscal-year', {
      params: { query: { accountingUnitKey: 'fsr-dit', key: '2025' } },
      body: { name: 'HHJ 25', startDate: '2025-01-01', endDate: '2025-12-31' },
    }),
  );
  expect(await screen.findByText('Haushaltsjahr wurde gespeichert')).toBeInTheDocument();
  expect(api.GET).toHaveBeenCalledWith('/api/get-user-access/my-user-access');
});

test('explains that receipts lie outside the changed period', async () => {
  api.PUT.mockResolvedValue(
    failed(409, { key: '2025', accountingUnitKey: 'fsr-dit', startDate: '2025-01-01', endDate: '2025-12-31' }),
  );
  renderAt('/haushaltsjahre-verwalten/fsr-dit');

  userEvent.click(await screen.findByRole('button', { name: 'Haushaltsjahr 2025 bearbeiten' }));
  const dialog = await screen.findByRole('dialog');
  userEvent.click(within(dialog).getByRole('button', { name: 'Speichern' }));

  expect(await within(dialog).findByText('Es gibt Belege außerhalb dieses Zeitraums')).toBeInTheDocument();
});
