import tailwindcss from '@tailwindcss/vite';
import { loadEnv } from 'vite';
import { defineConfig } from 'vitest/config';
import { playwright } from '@vitest/browser-playwright';
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/** The icons imported anywhere in the app, each one is a module of its own (e.g. '@lucide/svelte/icons/pencil') */
function findLucideIcons(directory: string): string[] {
	const icons = new Set<string>();
	for (const entry of readdirSync(directory, { recursive: true, withFileTypes: true })) {
		if (!entry.isFile() || !/\.(svelte|ts)$/.test(entry.name)) continue;
		const source = readFileSync(join(entry.parentPath, entry.name), 'utf8');
		for (const [icon] of source.matchAll(/@lucide\/svelte\/icons\/[a-z0-9-]+/g)) icons.add(icon);
	}
	return [...icons];
}

export default defineConfig(({ mode }) => {
	// All variables of the .env files, not only the VITE_ ones that end up in the browser bundle
	const env = loadEnv(mode, process.cwd(), '');

	return {
		plugins: [
			tailwindcss(),
			sveltekit({
				compilerOptions: {
					// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
					runes: ({ filename }) =>
						filename.split(/[/\\]/).includes('node_modules') ? undefined : true
				},
				// A single page app: nginx serves index.html for every route (see nginx.conf), the routing happens in the
				// browser. Nothing is rendered on a server, see ssr in src/routes/+layout.ts.
				adapter: adapter({ fallback: 'index.html' })
			})
		],
		server: {
			// Registered as redirect URI of the Keycloak client and as allowed CORS origin of the backend
			port: 3000,
			strictPort: true,
			// Lets the app use relative /api URLs like in production
			proxy: {
				'/api': {
					target: env.API_PROXY_TARGET || 'http://localhost:5114',
					changeOrigin: true
				}
			}
		},
		test: {
			expect: { requireAssertions: true },
			// Call history is per test, the mocked return values are set in each test file's beforeEach
			clearMocks: true,
			projects: [
				{
					// Component tests run in a real browser, so the bits-ui components behave like for the users
					extends: './vite.config.ts',
					// Bundled up front, a dependency first found while the tests run would reload them midway
					optimizeDeps: {
						include: [
							'@internationalized/date',
							...findLucideIcons('src'),
							'bits-ui',
							'cn',
							'openapi-fetch',
							'svelte-sonner',
							'tailwind-variants'
						]
					},
					test: {
						name: 'client',
						browser: {
							enabled: true,
							provider: playwright(),
							instances: [{ browser: 'chromium', headless: true }]
						},
						include: ['src/**/*.svelte.{test,spec}.{js,ts}'],
						setupFiles: ['./src/vitest-setup-client.ts']
					}
				},
				{
					extends: './vite.config.ts',
					test: {
						name: 'server',
						environment: 'node',
						include: ['src/**/*.{test,spec}.{js,ts}'],
						exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
					}
				}
			]
		}
	};
});
