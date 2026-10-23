export interface AuthState {
	/** Keycloak user name, e.g. the HTWK login. Cryptic. */
	username: string;
	/** Full name if Keycloak knows it, otherwise the user name */
	displayName: string;
	logout: () => Promise<void>;
}

let current = $state<AuthState>();

/** Set by the AuthGate once the user is logged in, tests set it directly */
export function setAuth(state: AuthState | undefined) {
	current = state;
}

/** The logged-in user, available inside the AuthGate */
export function getAuth(): AuthState {
	if (!current) throw new Error('getAuth must be used inside the AuthGate');
	return current;
}

/** The logged-in user, undefined until the login is done */
export function getAuthState(): AuthState | undefined {
	return current;
}
