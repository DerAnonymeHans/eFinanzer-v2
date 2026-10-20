import type { Mock } from 'vitest';
import { render, screen } from '@testing-library/react';
import apiClient from '../../api/client';
import { Provider } from 'react-redux';
import { makeStore } from '../../stores/store';
import UserName from './UserName';

vi.mock('../../api/client', () => ({ default: { GET: vi.fn() } }));
const get = apiClient.GET as Mock;

// Every test starts with an empty cache, rerenders within a test keep it
let store = makeStore();
beforeEach(() => {
  get.mockReset();
  store = makeStore();
});

test('shows the full name instead of the user name', async () => {
  get.mockResolvedValue({ data: { username: 'mmuster', firstName: 'Max', lastName: 'Muster' }, response: { ok: true } });

  render(
    <Provider store={store}>
      <UserName username="mmuster" />
    </Provider>,
  );

  expect(screen.getByLabelText('Name wird geladen')).toBeInTheDocument();
  expect(await screen.findByText('Max Muster')).toBeInTheDocument();
  expect(screen.queryByText('mmuster')).not.toBeInTheDocument();
  expect(get).toHaveBeenCalledWith('/api/get-users/get-user', { params: { query: { username: 'mmuster' } } });
});

test('loads every user only once', async () => {
  get.mockResolvedValue({ data: { username: 'mmuster', firstName: 'Max', lastName: 'Muster' }, response: { ok: true } });

  const { rerender } = render(
    <Provider store={store}>
      <UserName username="mmuster" />
      <UserName username="MMuster" />
    </Provider>,
  );
  expect(await screen.findAllByText('Max Muster')).toHaveLength(2);

  rerender(
    <Provider store={store}>
      <UserName username="mmuster" />
      <UserName username="MMuster" />
      <UserName username="mmuster" />
    </Provider>,
  );
  expect(await screen.findAllByText('Max Muster')).toHaveLength(3);
  expect(get).toHaveBeenCalledTimes(1);
});

test('falls back to the user name if the full name is not known', async () => {
  get.mockImplementation((_path: string, options?: { params: { query: { username: string } } }) =>
    Promise.resolve(
      options?.params.query.username === 'nameless'
        ? { data: { username: 'nameless', firstName: null, lastName: null }, response: { ok: true } }
        : { data: undefined, response: { ok: false, status: 404 } },
    ),
  );

  render(
    <Provider store={store}>
      <UserName username="nameless" />
      <UserName username="gone" />
    </Provider>,
  );

  expect(await screen.findByText('nameless')).toBeInTheDocument();
  expect(await screen.findByText('gone')).toBeInTheDocument();
});

test('retries a failed request the next time the user is shown', async () => {
  get.mockRejectedValueOnce(new Error('offline'));

  // Rerendering keeps the store and with it the cache
  const { rerender } = render(
    <Provider store={store}>
      <UserName username="mmuster" />
    </Provider>,
  );
  expect(await screen.findByText('mmuster')).toBeInTheDocument();

  get.mockResolvedValue({ data: { username: 'mmuster', firstName: 'Max', lastName: 'Muster' }, response: { ok: true } });
  rerender(<Provider store={store}>{null}</Provider>);
  rerender(
    <Provider store={store}>
      <UserName username="mmuster" />
    </Provider>,
  );

  expect(await screen.findByText('Max Muster')).toBeInTheDocument();
  expect(get).toHaveBeenCalledTimes(2);
});
