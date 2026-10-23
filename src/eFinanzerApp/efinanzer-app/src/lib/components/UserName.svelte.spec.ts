import { beforeEach, expect, test, vi, type Mock } from 'vitest';
import { page } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import apiClient from '#lib/api/client.js';
import { userCache } from '#lib/stores/user-cache.svelte.js';
import UserName from './UserName.svelte';

vi.mock('#lib/api/client.js', () => ({ default: { GET: vi.fn() } }));
const get = apiClient.GET as Mock;

// Every test starts with an empty cache, components rendered within a test share it
beforeEach(() => {
	get.mockReset();
	userCache.clear();
});

const maxMuster = { username: 'mmuster', firstName: 'Max', lastName: 'Muster' };

test('shows the full name instead of the user name', async () => {
	let resolve: (value: unknown) => void = () => {};
	get.mockReturnValue(new Promise((r) => (resolve = r)));

	render(UserName, { username: 'mmuster' });

	await expect.element(page.getByLabelText('Name wird geladen')).toBeInTheDocument();
	resolve({ data: maxMuster, response: { ok: true, status: 200 } });
	await expect.element(page.getByText('Max Muster')).toBeInTheDocument();
	await expect.element(page.getByText('mmuster')).not.toBeInTheDocument();
	expect(get).toHaveBeenCalledWith('/api/get-users/get-user', { params: { query: { username: 'mmuster' } } });
});

test('loads every user only once', async () => {
	get.mockResolvedValue({ data: maxMuster, response: { ok: true, status: 200 } });

	render(UserName, { username: 'mmuster' });
	render(UserName, { username: 'MMuster' });
	await expect.poll(() => page.getByText('Max Muster').all()).toHaveLength(2);

	render(UserName, { username: 'mmuster' });
	await expect.poll(() => page.getByText('Max Muster').all()).toHaveLength(3);
	expect(get).toHaveBeenCalledTimes(1);
});

test('falls back to the user name if the full name is not known', async () => {
	get.mockImplementation((_path: string, options: { params: { query: { username: string } } }) =>
		Promise.resolve(
			options.params.query.username === 'nameless'
				? { data: { username: 'nameless', firstName: null, lastName: null }, response: { ok: true, status: 200 } }
				: { data: undefined, response: { ok: false, status: 404 } }
		)
	);

	render(UserName, { username: 'nameless' });
	render(UserName, { username: 'gone' });

	await expect.element(page.getByText('nameless')).toBeInTheDocument();
	await expect.element(page.getByText('gone')).toBeInTheDocument();
});

test('retries a failed request the next time the user is shown', async () => {
	get.mockRejectedValueOnce(new Error('offline'));

	const { unmount } = render(UserName, { username: 'mmuster' });
	await expect.element(page.getByText('mmuster')).toBeInTheDocument();
	unmount();

	get.mockResolvedValue({ data: maxMuster, response: { ok: true, status: 200 } });
	render(UserName, { username: 'mmuster' });

	await expect.element(page.getByText('Max Muster')).toBeInTheDocument();
	expect(get).toHaveBeenCalledTimes(2);
});
