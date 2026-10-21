// Setup of the component tests, which run in the browser (see vite.config.ts)
import { vi } from 'vitest';

// The components are rendered outside of a SvelteKit app, so its modules are replaced by simple stand-ins.
// Tests that depend on the current page mock $app/state themselves.
vi.mock('$app/navigation', () => ({ goto: vi.fn() }));
vi.mock('$app/paths', () => ({
	asset: (file: string) => `/${file}`,
	resolve: (route: string, params: Record<string, string> = {}) =>
		route.replace(/\[(\w+)\]/g, (_, name: string) => params[name])
}));
