/// <reference types="vite/client" />

// Variables from the .env files that are available in the browser (only the VITE_ prefixed ones)
interface ImportMetaEnv {
  readonly VITE_KEYCLOAK_URL: string;
  readonly VITE_KEYCLOAK_REALM: string;
  readonly VITE_KEYCLOAK_CLIENT_ID: string;
  /** Base URL of the API, empty for the same origin (default) */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
