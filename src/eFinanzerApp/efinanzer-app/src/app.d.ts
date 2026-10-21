// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}

	// Variables from the .env files that are available in the browser (only the VITE_ prefixed ones)
	interface ImportMetaEnv {
		readonly VITE_KEYCLOAK_URL: string;
		readonly VITE_KEYCLOAK_REALM: string;
		readonly VITE_KEYCLOAK_CLIENT_ID: string;
		/** Base URL of the API, empty for the same origin (default) */
		readonly VITE_API_BASE_URL?: string;
	}
}

export {};
