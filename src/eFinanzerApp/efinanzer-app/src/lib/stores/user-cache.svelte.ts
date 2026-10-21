import { untrack } from 'svelte';
import { SvelteMap } from 'svelte/reactivity';
import apiClient from '../api/client';
import type { User } from '../api/types';
import { getUserDisplayName } from '../texts/usernames';

export type UserEntry =
	| { status: 'loading' }
	| { status: 'loaded'; user: User }
	| { status: 'notFound' }
	| { status: 'error' };

// User names are case-insensitive, so "mmuster" and "MMuster" share one entry
const toKey = (username: string) => username.toLowerCase();

/** Users by user name, each one loaded only once */
class UserCache {
	#users = new SvelteMap<string, UserEntry>();

	get(username: string): UserEntry | undefined {
		return this.#users.get(toKey(username));
	}

	/** Full name of the user, the user name while it is loading or if there is no full name */
	getDisplayName(username: string): string {
		const entry = this.get(username);
		return entry?.status === 'loaded' ? getUserDisplayName(entry.user) : username;
	}

	/** Loads the user unless it is already cached. A failed request is retried the next time. */
	async load(username: string): Promise<void> {
		const key = toKey(username);
		// Not tracked, so an effect calling this only reruns for another user name, not for every status change
		const entry = untrack(() => this.#users.get(key));
		if (entry !== undefined && entry.status !== 'error') return;

		this.#users.set(key, { status: 'loading' });
		try {
			const { data, response } = await apiClient.GET('/api/get-users/get-user', {
				params: { query: { username } }
			});
			if (response.status === 404) this.#users.set(key, { status: 'notFound' });
			else if (!response.ok) this.#users.set(key, { status: 'error' });
			else this.#users.set(key, data ? { status: 'loaded', user: data } : { status: 'notFound' });
		} catch {
			this.#users.set(key, { status: 'error' });
		}
	}

	/** Caches users that were already loaded elsewhere, e.g. by the user search */
	cacheUsers(users: User[]) {
		for (const user of users) {
			this.#users.set(toKey(user.username), { status: 'loaded', user });
		}
	}

	/** Forgets all users. For tests. */
	clear() {
		this.#users.clear();
	}
}

export const userCache = new UserCache();
