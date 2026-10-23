<script lang="ts">
	import type { LayoutProps } from './$types';
	import NoAccess from '#lib/components/NoAccess.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';

	let { children }: LayoutProps = $props();

	// Only users who may manage accounting units see these pages. The API checks it again for every request.
	const userAccess = useUserAccess();
</script>

<!-- Keeps the page while the user access is reloaded after a change, so its state survives -->
{#if userAccess.loading && !userAccess.value}
	<PageSpinner />
{:else if !userAccess.value?.manageAccountingUnits}
	<NoAccess description="Für die Verwaltung der Haushalte fehlt die Berechtigung." />
{:else}
	{@render children()}
{/if}
