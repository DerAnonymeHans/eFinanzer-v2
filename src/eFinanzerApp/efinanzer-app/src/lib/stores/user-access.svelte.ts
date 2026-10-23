import apiClient from '../api/client';
import type { UserAccess } from '../api/types';
import { DataLoader, type LoadFunctionResult } from '../api/data-loading.svelte';

async function fetchUserAccess(): LoadFunctionResult<UserAccess> {
	const { data, response } = await apiClient.GET('/api/get-user-access/my-user-access');
	return response.ok && data ? { value: data } : { error: response };
}

/** What the logged-in user may access. Loaded once and shared by all components. */
export const userAccessLoader = new DataLoader(fetchUserAccess, 'Benutzerrechte konnten nicht geladen werden');

/** What the logged-in user may access. Loads it on first use; call `load()` again e.g. after the user's roles changed. */
export function useUserAccess() {
	if (userAccessLoader.initialLoading && !userAccessLoader.loading) void userAccessLoader.load();
	return userAccessLoader;
}
