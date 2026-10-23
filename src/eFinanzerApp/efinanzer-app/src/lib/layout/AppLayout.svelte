<script lang="ts">
	import type { Snippet } from 'svelte';
	import MenuIcon from '@lucide/svelte/icons/menu';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Sheet from '#lib/components/ui/sheet/index.js';
	import { mobile } from '#lib/reactivity/is-mobile.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import Logo from './Logo.svelte';
	import SideNav from './SideNav.svelte';

	let { children }: { children: Snippet } = $props();

	let drawerOpen = $state(false);
	let selectedPageKey = $state('uebersicht');
	let accountingUnitKey = $state<string>();
	let fiscalYearKey = $state<string>();

	// The menu entries are not routed yet, so they only count as selected outside of the routed pages
	const onRoutedPage = $derived(page.url.pathname !== '/');
	const administrationSelected = $derived(page.url.pathname.startsWith('/administration'));
	// The fiscal year page belongs to one Haushalt, which is then also the one selected in the navigation
	const fiscalYearsPageKey = $derived(
		page.route.id === '/haushaltsjahre-verwalten/[accountingUnitKey]' ? page.params.accountingUnitKey : undefined
	);

	// Only Haushalte and fiscal years the user has access to can be selected. Without a valid choice the first one is
	// used, e.g. right after the user access was loaded.
	const userAccess = useUserAccess();
	const accountingUnits = $derived(userAccess.value?.accountingUnits ?? []);
	const accountingUnit = $derived.by(() => {
		const selectedKey = fiscalYearsPageKey ?? accountingUnitKey;
		return accountingUnits.find((unit) => unit.key === selectedKey) ?? accountingUnits[0];
	});
	const fiscalYear = $derived(
		accountingUnit?.fiscalYears.find((year) => year.key === fiscalYearKey) ?? accountingUnit?.fiscalYears[0]
	);

	function handleAccountingUnitChange(key: string) {
		accountingUnitKey = key;
		// Every Haushalt has its own fiscal years
		fiscalYearKey = undefined;
		// Switches to the fiscal years of the new Haushalt, if the user may manage them
		if (fiscalYearsPageKey !== undefined) {
			const canManage = accountingUnits.find((unit) => unit.key === key)?.manageFiscalYears ?? false;
			void goto(
				canManage ? resolve('/haushaltsjahre-verwalten/[accountingUnitKey]', { accountingUnitKey: key }) : resolve('/')
			);
		}
	}

	function handlePageKeySelect(key: string) {
		selectedPageKey = key;
		drawerOpen = false;
		void goto(resolve('/'));
	}

	function handleEditFiscalYear() {
		if (!accountingUnit) return;
		// Keeps the Haushalt selected after leaving the page
		accountingUnitKey = accountingUnit.key;
		drawerOpen = false;
		void goto(resolve('/haushaltsjahre-verwalten/[accountingUnitKey]', { accountingUnitKey: accountingUnit.key }));
	}
</script>

{#snippet nav()}
	<SideNav
		selectedPageKey={onRoutedPage ? undefined : selectedPageKey}
		onPageKeySelect={handlePageKeySelect}
		{accountingUnits}
		{accountingUnit}
		onAccountingUnitChange={handleAccountingUnitChange}
		{fiscalYear}
		onFiscalYearChange={(key) => (fiscalYearKey = key)}
		onNavigate={() => (drawerOpen = false)}
		onEditFiscalYear={handleEditFiscalYear}
		{administrationSelected}
	/>
{/snippet}

<div class="flex min-h-screen">
	{#if mobile.current}
		<Sheet.Root bind:open={drawerOpen}>
			<Sheet.Content
				side="left"
				showCloseButton={false}
				class="w-[280px] overflow-y-auto border-sidebar-border bg-sidebar p-0 sm:max-w-[280px]"
			>
				<Sheet.Title class="sr-only">Navigation</Sheet.Title>
				{@render nav()}
			</Sheet.Content>
		</Sheet.Root>
	{:else}
		<!-- The navigation stays in place while the main area scrolls -->
		<aside class="sticky top-0 h-screen w-60 flex-none overflow-y-auto bg-sidebar">
			{@render nav()}
		</aside>
	{/if}

	<div class="flex min-w-0 flex-1 flex-col">
		{#if mobile.current}
			<header class="sticky top-0 z-10 flex h-14 items-center gap-2 bg-sidebar px-3">
				<Button
					variant="ghost"
					size="icon-lg"
					class="size-11 text-sidebar-foreground hover:bg-sidebar-accent hover:text-white [&_svg:not([class*='size-'])]:size-5"
					aria-label="Navigation öffnen"
					onclick={() => (drawerOpen = true)}
				>
					<MenuIcon />
				</Button>
				<span class="inline-flex items-center gap-2 text-[17px] font-bold tracking-[-0.01em] text-sidebar-foreground">
					<Logo size={22} />
					eFinanzer
				</span>
			</header>
		{/if}

		<main class="min-w-0 flex-1 px-4 pt-5 pb-10 md:px-10 md:pt-8 md:pb-14">
			{@render children()}
		</main>
	</div>
</div>
