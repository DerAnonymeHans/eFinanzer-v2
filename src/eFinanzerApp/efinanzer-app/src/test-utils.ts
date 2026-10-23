// Helpers for the component tests (*.svelte.spec.ts)
import { createRawSnippet } from 'svelte';
import { vi } from 'vitest';
import { render } from 'vitest-browser-svelte';
import { Toaster } from 'svelte-sonner';
import ConfirmDialog from '#lib/components/confirm-dialog/ConfirmDialog.svelte';
import type { UserAccess } from '#lib/api/types.js';
import { setAuth } from '#lib/auth/auth.svelte.js';
import { userAccessLoader } from '#lib/stores/user-access.svelte.js';
import { userCache } from '#lib/stores/user-cache.svelte.js';

/** A successful response of the mocked API client */
export function ok<T>(data?: T, status = 200) {
	return { data, response: { ok: true, status } };
}

/** A failed response of the mocked API client */
export function failed(status: number, error: unknown = {}) {
	return { data: undefined, error, response: { ok: false, status } };
}

/**
 * Starts a test as Max Muster with an empty user cache and, if given, an already loaded user access.
 * Without one the user access is loaded through the (mocked) API on first use.
 */
export function resetApp(userAccess?: UserAccess) {
	setAuth({ username: 'mmuster', displayName: 'Max Muster', logout: vi.fn().mockResolvedValue(undefined) });
	userAccessLoader.reset(userAccess);
	userCache.clear();
}

/** Content for layouts and other components that render children */
export const childrenText = (text: string) => createRawSnippet(() => ({ render: () => `<p>${text}</p>` }));

/** Renders the confirm dialog and the toasts, which the root layout renders once for the whole app */
export function renderAppOverlays() {
	render(ConfirmDialog);
	render(Toaster);
}
