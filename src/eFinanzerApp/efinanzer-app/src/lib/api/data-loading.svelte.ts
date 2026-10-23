import { untrack } from 'svelte';

/** The loaded value, or why loading failed: a message or the failed response (shown with its status code) */
export type LoadFunctionResult<TValue> = Promise<{ value: TValue } | { error: string | Response }>;
type LoadFunction<TValue, TArgs extends unknown[]> = (...args: TArgs) => LoadFunctionResult<TValue>;

/**
 * Loads the data of a page and keeps it together with its loading and error state.
 * Only the latest load may update the state, so a slow earlier response cannot overwrite a newer one.
 */
export class DataLoader<TValue, TArgs extends unknown[] = []> {
	value = $state.raw<TValue>();
	error = $state<string>();
	loading = $state(false);

	#latestRequest = 0;
	readonly #loadFunction: LoadFunction<TValue, TArgs>;
	/** Default failure message if the load function does not provide one */
	readonly #failureMessage: string;

	constructor(loadFunction: LoadFunction<TValue, TArgs>, failureMessage: string) {
		this.#loadFunction = loadFunction;
		this.#failureMessage = failureMessage;
	}

	/** True until the first load is done */
	get initialLoading() {
		return this.value === undefined && this.error === undefined;
	}

	/** Returns false if a newer load was started in the meantime. This result is then dropped */
	async load(...args: TArgs): Promise<boolean> {
		const request = ++this.#latestRequest;
		this.loading = true;

		let value: TValue | undefined;
		let error: string | undefined;
		try {
			const result = await untrack(() => this.#loadFunction(...args));
			if ('value' in result) value = result.value;
			else if (typeof result.error === 'string') error = result.error;
			else error = `${this.#failureMessage} (HTTP ${result.error.status})`;
		} catch {
			error = this.#failureMessage;
		}

		if (request !== this.#latestRequest) return false;
		this.value = value;
		this.error = error;
		this.loading = false;
		return true;
	}

	/** Starts over, optionally with an already loaded value, and drops the result of a running load. For tests. */
	reset(value?: TValue) {
		this.#latestRequest++;
		this.value = value;
		this.error = undefined;
		this.loading = false;
	}
}

export function useDataLoader<TValue, TArgs extends unknown[] = []>(
	loadFunction: LoadFunction<TValue, TArgs>,
	failureMessage: string
) {
	return new DataLoader(loadFunction, failureMessage);
}
