# eFinanzer App

React frontend of eFinanzer, built with [Vite](https://vite.dev), TypeScript and [Ant Design](https://ant.design).
Requires Node 22.12 or newer.

## Scripts

| Command | Description |
|---|---|
| `npm run dev` | Dev server on http://localhost:3000, forwards `/api` to the backend |
| `npm run build` | Type check and production build into `dist/` |
| `npm run preview` | Serves the production build locally |
| `npm test` | Tests with Vitest (watch mode, `npx vitest run` for a single run) |
| `npm run lint` | ESLint |
| `npm run typecheck` | TypeScript check only |
| `npm run generate:api` | API types from the backend's `OpenApi/efinanzer.json` (updated on Debug builds) |
| `npm run generate:api:url -- <url>` | API types from a running backend, e.g. `https://efinanzer.stura.htwk-leipzig.de/openapi/v1.json` |

## Configuration

`.env` holds the defaults, put local overrides into `.env.local` (not committed).

| Variable | Description |
|---|---|
| `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID` | Keycloak login (public client `efinanzer-app`) |
| `VITE_API_BASE_URL` | API origin, empty for the same origin as the app (production, or the dev proxy) |
| `API_PROXY_TARGET` | Target of the dev server's `/api` proxy, defaults to `http://localhost:5114` |
