import type { Snippet } from 'svelte';

export interface ConfirmOptions {
	title: string;
	/** Text or markup below the title */
	description?: string | Snippet;
	confirmText: string;
	/** Colors the confirm button red, e.g. to delete something */
	destructive?: boolean;
	/** Runs with a spinner on the confirm button, the dialog closes once it is done */
	onConfirm: () => Promise<void> | void;
}

class ConfirmState {
	options = $state<ConfirmOptions>();
	open = $state(false);
	pending = $state(false);
}

/** State of the one confirm dialog of the app, rendered by the ConfirmDialog in the root layout */
export const confirmState = new ConfirmState();

/** Asks the user to confirm an action before it runs */
export function confirm(options: ConfirmOptions) {
	confirmState.options = options;
	confirmState.pending = false;
	confirmState.open = true;
}
