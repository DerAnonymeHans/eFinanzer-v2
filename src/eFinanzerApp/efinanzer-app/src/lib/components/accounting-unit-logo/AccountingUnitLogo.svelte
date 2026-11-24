<script lang="ts">
	import LandmarkIcon from '@lucide/svelte/icons/landmark';
	import { accountingUnitLogoUrl } from './accounting-unit-logo.svelte.js';
	import { cn } from '#lib/type-utils.js';

	interface Props {
		/** Key of the accounting unit whose logo is shown. Without one, only the placeholder is shown. */
		accountingUnitKey?: string;
		/** Shown instead of the stored logo, e.g. the preview of a file to upload */
		src?: string;
		/** Size and shape, overrides the defaults (a small rounded square) */
		class?: string;
	}

	let { accountingUnitKey, src, class: className }: Props = $props();

	const url = $derived(src ?? (accountingUnitKey ? accountingUnitLogoUrl(accountingUnitKey) : undefined));

	/** The URL that could not be loaded, e.g. because the unit has no logo */
	let failedUrl = $state<string>();
	const showPlaceholder = $derived(!url || failedUrl === url);
</script>

<!-- Decorative, the name of the accounting unit is always shown next to it -->
<div
	class={cn(
		'flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-md border',
		showPlaceholder ? 'bg-muted text-muted-foreground' : 'bg-background',
		className
	)}
	data-testid="accounting-unit-logo"
>
	{#if showPlaceholder}
		<LandmarkIcon class="size-1/2 max-h-16 max-w-16" aria-hidden="true" />
	{:else}
		<img src={url} alt="" class="size-full object-contain" onerror={() => (failedUrl = url)} />
	{/if}
</div>
