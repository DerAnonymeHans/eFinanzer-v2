<script lang="ts">
	import type { LayoutProps } from './$types';
	import NoAccess from '#lib/components/NoAccess.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';

	let { children, params }: LayoutProps = $props();

	// Only users who may manage the fiscal years of this Haushalt see the page. The API checks it again for every request.
	const access = useUserAccess();
	const accountingUnit = $derived(
		access.userAccess?.accountingUnits.find((unit) => unit.key === params.accountingUnitKey)
	);
</script>

<!-- Keeps the page while the user access is reloaded after a change, so its state survives -->
{#if access.loading && !access.userAccess}
	<PageSpinner />
{:else if !accountingUnit?.manageFiscalYears}
	<NoAccess description="Für die Verwaltung der Haushaltsjahre dieses Haushalts fehlt die Berechtigung." />
{:else}
	{@render children()}
{/if}
