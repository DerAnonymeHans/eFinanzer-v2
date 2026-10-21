import apiClient from '../api/client';
import type { UserAccess } from '../api/types';

type Status = 'idle' | 'loading' | 'loaded' | 'failed';

/** What the logged-in user may access. Loaded once and shared by all components. */
class UserAccessStore {
	/** Undefined until loaded. A failed reload keeps the last loaded access. */
	userAccess = $state<UserAccess>();
	status = $state<Status>('idle');
	error = $state<string>();

	// Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
	#latestRequest = 0;

	get loading() {
		return this.status === 'idle' || this.status === 'loading';
	}

	/** Loads the user access, unless it was already loaded or is loading */
	load() {
		if (this.status === 'idle') void this.reload();
	}

	/** Loads the user access again, e.g. after the user's roles changed */
	async reload() {
		const request = ++this.#latestRequest;
		this.status = 'loading';

		let result: UserAccess | undefined;
		let failure: string | undefined;
		try {
			const { data, response } = await apiClient.GET('/api/get-user-access/my-user-access');
			if (response.ok && data) result = data;
			else failure = `Benutzerrechte konnten nicht geladen werden (HTTP ${response.status})`;
		} catch (e) {
			failure = e instanceof Error ? e.message : String(e);
		}

		if (request !== this.#latestRequest) return;
		if (result) {
			this.userAccess = result;
			this.status = 'loaded';
			this.error = undefined;
		} else {
			this.status = 'failed';
			this.error = failure;
		}
	}

	/** Starts over, optionally with an already loaded access. For tests. */
	reset(userAccess?: UserAccess) {
		this.#latestRequest++;
		this.userAccess = userAccess;
		this.status = userAccess ? 'loaded' : 'idle';
		this.error = undefined;
	}
}

const userAccessStore = new UserAccessStore();

/** What the logged-in user may access. Loads it on first use and shares it between all components. */
export function useUserAccess() {
	userAccessStore.load();
	return userAccessStore;
}

export { userAccessStore };
