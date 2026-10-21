<script lang="ts">
	import type { LayoutProps } from './$types';
	import NoAccess from '#lib/components/NoAccess.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';

	let { children }: LayoutProps = $props();

	// Only users who may manage accounting units see these pages. The API checks it again for every request.
	const access = useUserAccess();
</script>

<!-- Keeps the page while the user access is reloaded after a change, so its state survives -->
{#if access.loading && !access.userAccess}
	<PageSpinner />
{:else if !access.userAccess?.manageAccountingUnits}
	<NoAccess description="Für die Verwaltung der Haushalte fehlt die Berechtigung." />
{:else}
	{@render children()}
{/if}
