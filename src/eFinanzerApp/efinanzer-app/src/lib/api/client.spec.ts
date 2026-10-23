import { expect, test, vi } from 'vitest';
import type { Mock } from 'vitest';
import apiClient from './client';
import { getAccessToken } from '../auth/keycloak';

vi.mock('../auth/keycloak', () => ({ getAccessToken: vi.fn() }));
const accessToken = getAccessToken as Mock;

function jsonResponse(body: unknown) {
	return new Response(JSON.stringify(body), {
		status: 200,
		headers: { 'Content-Type': 'application/json' },
	});
}

test('sends requests to the generated API paths with the access token', async () => {
	accessToken.mockResolvedValue('token-123');
	const fetch = vi.fn().mockResolvedValue(jsonResponse({ userName: 'max' }));

	const { data } = await apiClient.GET('/api/get-users/get-user', {
		params: { query: { username: 'max' } },
		baseUrl: 'http://localhost',
		fetch,
	});

	const request: Request = fetch.mock.calls[0][0];
	expect(request.url).toBe('http://localhost/api/get-users/get-user?username=max');
	expect(request.headers.get('Authorization')).toBe('Bearer token-123');
	expect(data).toEqual({ userName: 'max' });
});

test('sends no Authorization header without a login', async () => {
	accessToken.mockResolvedValue(undefined);
	const fetch = vi.fn().mockResolvedValue(jsonResponse({}));

	await apiClient.GET('/api/get-user-access/my-user-access', { baseUrl: 'http://localhost', fetch });

	const request: Request = fetch.mock.calls[0][0];
	expect(request.headers.has('Authorization')).toBe(false);
});
