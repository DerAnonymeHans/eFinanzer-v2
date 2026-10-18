import Keycloak from 'keycloak-js';

// Configured in .env, can be overridden per environment (e.g. .env.local)
const keycloak = new Keycloak({
  url: process.env.REACT_APP_KEYCLOAK_URL!,
  realm: process.env.REACT_APP_KEYCLOAK_REALM!,
  clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID!,
});

let initialization: Promise<boolean> | undefined;

/**
 * Starts the login: redirects to Keycloak if there is no session yet, otherwise resolves once the tokens are loaded.
 * A Keycloak instance may only be initialized once, so repeated calls (e.g. from React's StrictMode) share the result.
 */
export function initKeycloak(): Promise<boolean> {
  initialization ??= keycloak.init({
    onLoad: 'login-required',
    pkceMethod: 'S256',
    // The session iframe needs third-party cookies, which most browsers block by now
    checkLoginIframe: false,
  });
  return initialization;
}

/**
 * Returns a valid access token, refreshing it first if it expires within the next 30 seconds.
 * Undefined while the user is not logged in.
 */
export async function getAccessToken(): Promise<string | undefined> {
  if (!keycloak.authenticated) return undefined;
  try {
    await keycloak.updateToken(30);
  } catch {
    // The session ended (e.g. logged out elsewhere), so a new login is needed
    await keycloak.login();
    return undefined;
  }
  return keycloak.token;
}

export default keycloak;
