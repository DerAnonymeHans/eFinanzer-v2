import createClient from 'openapi-fetch';
import type { paths } from './schema';

/**
 * Typed client for the eFinanzer API.
 * The types in schema.d.ts are generated from the backend's OpenAPI document via `npm run generate:api`.
 *
 * Frontend and API are served from the same origin (see src/deploy/nginx.conf), so requests use relative URLs by
 * default. During development the CRA dev server proxies them to the backend (see src/setupProxy.js).
 *
 * @example
 * const { data, error } = await apiClient.GET('/api/get-users/get-user', {
 *   params: { query: { username } },
 * });
 */
const apiClient = createClient<paths>({
  baseUrl: process.env.REACT_APP_API_BASE_URL ?? '',
});

export default apiClient;
