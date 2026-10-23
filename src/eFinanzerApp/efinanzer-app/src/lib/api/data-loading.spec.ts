import { expect, test } from 'vitest';
import { DataLoader, type LoadFunctionResult } from './data-loading.svelte';

/** A load function whose calls are resolved or rejected by the test, in any order */
function controlledLoadFunction<TValue>() {
	const calls: { resolve: (result: Awaited<LoadFunctionResult<TValue>>) => void; reject: (e: Error) => void }[] =
		[];
	const load = () => new Promise<Awaited<LoadFunctionResult<TValue>>>((resolve, reject) => calls.push({ resolve, reject }));
	return { load, calls };
}

test('keeps the loaded value', async () => {
	const loader = new DataLoader(async () => ({ value: 'Haushalte' }), 'Fehler');
	expect(loader.initialLoading).toBe(true);

	expect(await loader.load()).toBe(true);

	expect(loader.value).toBe('Haushalte');
	expect(loader.error).toBeUndefined();
	expect(loader.loading).toBe(false);
	expect(loader.initialLoading).toBe(false);
});

test('shows the status code of a failed response and drops the earlier value', async () => {
	let fails = false;
	const loader = new DataLoader(
		async (): LoadFunctionResult<string> =>
			fails ? { error: new Response(null, { status: 500 }) } : { value: 'Haushalte' },
		'Haushalte konnten nicht geladen werden'
	);
	await loader.load();

	fails = true;
	await loader.load();

	expect(loader.value).toBeUndefined();
	expect(loader.error).toBe('Haushalte konnten nicht geladen werden (HTTP 500)');
});

test('shows an error message of the load function as it is', async () => {
	const loader = new DataLoader(async () => ({ error: 'Den Haushalt gibt es nicht' }), 'Fehler');

	await loader.load();

	expect(loader.error).toBe('Den Haushalt gibt es nicht');
});

test('stops loading if the load function throws', async () => {
	const loader = new DataLoader(() => Promise.reject(new Error('offline')), 'Haushalte konnten nicht geladen werden');

	await loader.load();

	expect(loader.error).toBe('Haushalte konnten nicht geladen werden');
	expect(loader.loading).toBe(false);
});

test('passes its arguments to the load function', async () => {
	const loader = new DataLoader(async (key: string) => ({ value: `Haushalt ${key}` }), 'Fehler');

	await loader.load('fsr-dit');

	expect(loader.value).toBe('Haushalt fsr-dit');
});

test('ignores an earlier load that finishes after a newer one', async () => {
	const { load, calls } = controlledLoadFunction<string>();
	const loader = new DataLoader(load, 'Fehler');
	const first = loader.load();
	const second = loader.load();

	calls[1].resolve({ value: 'neu' });
	expect(await second).toBe(true);
	calls[0].resolve({ value: 'alt' });
	expect(await first).toBe(false);

	expect(loader.value).toBe('neu');
});

test('ignores an earlier load that fails after a newer one', async () => {
	const { load, calls } = controlledLoadFunction<string>();
	const loader = new DataLoader(load, 'Fehler');
	const first = loader.load();
	const second = loader.load();

	calls[1].resolve({ value: 'neu' });
	await second;
	calls[0].reject(new Error('offline'));
	await first;

	expect(loader.value).toBe('neu');
	expect(loader.error).toBeUndefined();
});

test('keeps loading while a newer load is running', async () => {
	const { load, calls } = controlledLoadFunction<string>();
	const loader = new DataLoader(load, 'Fehler');
	const first = loader.load();
	void loader.load();

	calls[0].resolve({ value: 'alt' });
	await first;

	expect(loader.loading).toBe(true);
	expect(loader.value).toBeUndefined();
});
