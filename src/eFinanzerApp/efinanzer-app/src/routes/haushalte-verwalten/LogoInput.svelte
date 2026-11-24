<script lang="ts">
	import ImageUpIcon from '@lucide/svelte/icons/image-up';
	import XIcon from '@lucide/svelte/icons/x';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Field from '#lib/components/ui/field/index.js';
	import AccountingUnitLogo from '#lib/components/accounting-unit-logo/AccountingUnitLogo.svelte';
	import { LOGO_ACCEPT } from '#lib/components/accounting-unit-logo/accounting-unit-logo.svelte.js';
	import { toFieldErrors } from '#lib/utils.js';

	interface Props {
		id: string;
		/** The selected file, uploaded by the form. Null keeps the current logo. */
		file: File | null;
		/** Key of the edited accounting unit, its current logo is shown until a file is selected */
		accountingUnitKey?: string;
		error?: string;
		/** Size of the logo preview */
		logoClass?: string;
	}

	let { id, file = $bindable(), accountingUnitKey, error, logoClass = 'size-32' }: Props = $props();

	let input = $state<HTMLInputElement>();

	// Preview of the selected file, released again when another one is selected
	const previewUrl = $derived(file ? URL.createObjectURL(file) : undefined);
	$effect(() => {
		const url = previewUrl;
		return () => {
			if (url) URL.revokeObjectURL(url);
		};
	});

	function handleChange() {
		file = input?.files?.[0] ?? null;
	}

	function clear() {
		file = null;
		if (input) input.value = '';
	}
</script>

<Field.Field data-invalid={!!error || undefined}>
	<Field.Label for={id}>
		Logo <span class="font-normal text-muted-foreground">(optional)</span>
	</Field.Label>
	<div class="flex items-start gap-4">
		<AccountingUnitLogo {accountingUnitKey} src={previewUrl} class={`${logoClass} p-2`} />
		<div class="flex flex-col items-start gap-2">
			<Button type="button" variant="outline" onclick={() => input?.click()}>
				<ImageUpIcon />
				<!-- When a key exists it is an update, else a creation	-->
				{accountingUnitKey ? 'Logo ändern' : 'Logo auswählen'}
			</Button>
			{#if file}
				<Button type="button" variant="ghost" onclick={clear}>
					<XIcon />
					Auswahl verwerfen
				</Button>
			{/if}
		</div>
	</div>
	<!-- Hidden, the button opens it. Stays in the accessibility tree, so the label still names it. -->
	<input
		bind:this={input}
		{id}
		type="file"
		accept={LOGO_ACCEPT}
		class="sr-only"
		tabindex="-1"
		aria-invalid={!!error || undefined}
		onchange={handleChange}
	/>
	<Field.Description>
		{#if file}
			{file.name}
		{:else}
			PNG, JPG oder SVG, höchstens 5 MB
		{/if}
	</Field.Description>
	<Field.Error errors={toFieldErrors(error)} />
</Field.Field>
