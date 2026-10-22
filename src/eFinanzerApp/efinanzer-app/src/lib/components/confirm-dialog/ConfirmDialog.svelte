<script lang="ts">
	import * as AlertDialog from '#lib/components/ui/alert-dialog/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import { confirmState } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';

	const options = $derived(confirmState.options);
	const description = $derived(options?.description);

	async function handleConfirm() {
		if (!options) return;
		confirmState.pending = true;
		try {
			await options.onConfirm();
		} finally {
			confirmState.pending = false;
			confirmState.open = false;
		}
	}
</script>

<!-- The dialog behind confirm(), rendered once in the root layout -->
<AlertDialog.Root
	bind:open={() => confirmState.open, (open) => (confirmState.open = open || confirmState.pending)}
>
	<AlertDialog.Content>
		{#if options}
			<AlertDialog.Header>
				<AlertDialog.Title>{options.title}</AlertDialog.Title>
				{#if typeof description === 'string'}
					<AlertDialog.Description>{description}</AlertDialog.Description>
				{:else if description}
					<AlertDialog.Description>
						{#snippet child({ props })}
							<div {...props}>{@render description()}</div>
						{/snippet}
					</AlertDialog.Description>
				{/if}
			</AlertDialog.Header>
			<AlertDialog.Footer>
				<AlertDialog.Cancel disabled={confirmState.pending}>Abbrechen</AlertDialog.Cancel>
				<Button
					variant={options.destructive ? 'destructive-solid' : 'default'}
					disabled={confirmState.pending}
					onclick={handleConfirm}
				>
					{#if confirmState.pending}<Spinner />{/if}
					{options.confirmText}
				</Button>
			</AlertDialog.Footer>
		{/if}
	</AlertDialog.Content>
</AlertDialog.Root>
