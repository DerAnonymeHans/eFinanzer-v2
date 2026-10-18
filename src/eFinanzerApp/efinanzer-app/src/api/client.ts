import createClient from 'openapi-fetch';
import type { paths } from './schema';
import { getAccessToken } from '../auth/keycloak';

/**
 * Typed client for the eFinanzer API.
 * The types in schema.d.ts are generated from the backend's OpenAPI document via `npm run generate:api`.
 *
 * Frontend and API are served from the same origin (see src/deploy/nginx.conf), so requests use relative URLs by
 * default. During development the CRA dev server proxies them to the backend (see src/setupProxy.js).
 *
 * Every request carries the Keycloak access token of the logged-in user.
 *
 * @example
 * const { data, error } = await apiClient.GET('/api/get-users/get-user', {
 *   params: { query: { username } },
 * });
 */
const apiClient = createClient<paths>({
  baseUrl: process.env.REACT_APP_API_BASE_URL ?? '',
});

apiClient.use({
  async onRequest({ request }) {
    const token = await getAccessToken();
    if (token) request.headers.set('Authorization', `Bearer ${token}`);
    return request;
  },
});

export default apiClient;
