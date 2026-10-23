# eFinanzer App

Frontend of eFinanzer, built with [SvelteKit](https://svelte.dev/docs/kit) (Svelte 5), TypeScript,
[Tailwind CSS](https://tailwindcss.com) and [shadcn-svelte](https://shadcn-svelte.com).
Requires Node 22.12 or newer.

It is a single page app: `adapter-static` builds it into `build/`, nginx serves `index.html` for every route (see
`nginx.conf`) and the routing happens in the browser. Nothing is rendered on a server.

## Scripts

| Command | Description |
|---|---|
| `npm run dev` | Dev server on http://localhost:3000, forwards `/api` to the backend |
| `npm run build` | Type check and production build into `build/` |
| `npm run preview` | Serves the production build locally |
| `npm test` | Tests with Vitest (watch mode, `npm test -- --run` for a single run) |
| `npm run lint` | ESLint |
| `npm run check` | Type check only (svelte-check) |
| `npm run generate:api` | API types from the backend's `OpenApi/efinanzer.json` (updated on Debug builds) |
| `npm run generate:api:url -- <url>` | API types from a running backend, e.g. `https://efinanzer.stura.htwk-leipzig.de/openapi/v1.json` |

The component tests (`*.svelte.spec.ts`) run in Chromium through Playwright. Install it once with
`npx playwright install chromium`. The other tests (`*.spec.ts`) run in Node.

## Structure

| Path | Content |
|---|---|
| `src/routes` | Pages, one folder per URL. Components and helpers used by only one page live next to it. |
| `src/lib/components/ui` | shadcn-svelte components, added with `npx shadcn-svelte@latest add <name>` and owned by this repo |
| `src/lib/components` | Components shared by several pages |
| `src/lib/layout` | Login, side navigation and page frame |
| `src/lib/stores` | State shared by all pages (user access, user cache) |
| `src/lib/api` | Typed API client, `schema.d.ts` is generated |

Shared code is imported through the `#lib` alias, e.g. `import { Button } from '#lib/components/ui/button/index.js'`.
The design tokens (colors, font, radius) are CSS variables in `src/routes/layout.css`.

## Configuration

`.env` holds the defaults, put local overrides into `.env.local` (not committed).

| Variable | Description |
|---|---|
| `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID` | Keycloak login (public client `efinanzer-app`) |
| `VITE_API_BASE_URL` | API origin, empty for the same origin as the app (production, or the dev proxy) |
| `API_PROXY_TARGET` | Target of the dev server's `/api` proxy, defaults to `http://localhost:5114` |
