<script lang="ts" module>
	export const navItems = [
		{ key: 'uebersicht', label: 'Übersicht' },
		{ key: 'abrechnungen', label: 'Abrechnungen' },
		{ key: 'haushaltsplan', label: 'Haushaltsplan' }
	];
</script>

<script lang="ts">
	import LogOutIcon from '@lucide/svelte/icons/log-out';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import { resolve } from '$app/paths';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Select from '#lib/components/ui/select/index.js';
	import { Separator } from '#lib/components/ui/separator/index.js';
	import type { AccountingUnit, FiscalYear } from '#lib/api/types.js';
	import { getAuth } from '#lib/auth/auth.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { getRoleNames } from '#lib/texts/roles.js';
	import { cn } from '#lib/type-utils.js';
	import Logo from './Logo.svelte';

	const MAX_SHOWN_ROLES = 2;

	interface Props {
		/** Undefined while a page outside of the menu is shown */
		selectedPageKey: string | undefined;
		onPageKeySelect: (key: string) => void;
		accountingUnits: AccountingUnit[];
		accountingUnit: AccountingUnit | undefined;
		onAccountingUnitChange: (key: string) => void;
		fiscalYear: FiscalYear | undefined;
		onFiscalYearChange: (key: string) => void;
		/** Called before a link of the navigation is followed, e.g. to close the mobile drawer */
		onNavigate: () => void;
		onEditFiscalYear: () => void;
		administrationSelected: boolean;
	}

	let {
		selectedPageKey,
		onPageKeySelect,
		accountingUnits,
		accountingUnit,
		onAccountingUnitChange,
		fiscalYear,
		onFiscalYearChange,
		onNavigate,
		onEditFiscalYear,
		administrationSelected
	}: Props = $props();

	const auth = getAuth();
	const access = useUserAccess();
	const canEditAccountingUnits = $derived(access.userAccess?.manageAccountingUnits ?? false);
	const canEditFiscalYears = $derived(accountingUnit?.manageFiscalYears ?? false);
	const canAdministrate = $derived(access.userAccess?.resolveRoleChangeRequests ?? false);
	const roleNames = $derived(
		access.userAccess ? getRoleNames(access.userAccess).slice(0, MAX_SHOWN_ROLES) : []
	);
	const fiscalYears = $derived(accountingUnit?.fiscalYears ?? []);

	const menuItemClass =
		'flex h-11 w-full items-center rounded-lg px-3 text-left font-medium text-sidebar-foreground transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 focus-visible:ring-sidebar-ring focus-visible:outline-none';
	const selectedMenuItemClass = 'bg-sidebar-primary font-semibold text-sidebar-primary-foreground hover:bg-sidebar-primary';
	const labelClass = 'px-0.5 text-xs font-semibold tracking-[0.06em] text-sidebar-muted-foreground uppercase';
	const editButtonClass =
		'size-6 text-sidebar-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground';
	const selectTriggerClass =
		'w-full border-sidebar-border bg-sidebar-input font-semibold text-sidebar-foreground data-placeholder:text-sidebar-muted-foreground [&_svg]:text-sidebar-muted-foreground';
</script>

<!-- Content of the side navigation, shared by the desktop sidebar and the mobile drawer -->
<nav class="flex min-h-full flex-col gap-5 px-4.5 py-7 text-sidebar-foreground" aria-label="Hauptnavigation">
	<div class="flex flex-col gap-1 px-2.5">
		<a href="/" class="inline-flex items-center gap-2 text-[19px] font-bold tracking-[-0.01em]">
			<Logo size={24} />
			eFinanzer
		</a>
		<span class="text-[13px] text-sidebar-muted-foreground">Finanzverwaltung der Studierendenschaft</span>
	</div>

	<!-- Grouped, so the menu item sits closer to its separators than the gap between the sections -->
	<div class="flex flex-col gap-2">
		<Separator class="bg-sidebar-border" />
		{#if canAdministrate}
			<a
				href={resolve('/administration')}
				class={cn(menuItemClass, administrationSelected && selectedMenuItemClass)}
				aria-current={administrationSelected ? 'page' : undefined}
				onclick={onNavigate}
			>
				Administration
			</a>
			<Separator class="bg-sidebar-border" />
		{/if}
	</div>

	<div class="flex flex-col gap-3">
		<div class="flex flex-col gap-1">
			<div class="flex h-6 items-center justify-between">
				<label for="side-nav-accounting-unit" class={labelClass}>Haushalt</label>
				{#if canEditAccountingUnits}
					<Button
						variant="ghost"
						size="icon-xs"
						class={editButtonClass}
						href={resolve('/haushalte-verwalten')}
						aria-label="Haushalt bearbeiten"
						title="Haushalte verwalten"
						onclick={onNavigate}
					>
						<PencilIcon />
					</Button>
				{/if}
			</div>
			<Select.Root
				type="single"
				bind:value={() => accountingUnit?.key ?? '', onAccountingUnitChange}
				disabled={accountingUnits.length === 0}
			>
				<Select.Trigger id="side-nav-accounting-unit" class={selectTriggerClass}>
					{accountingUnit
						? (accountingUnit.name ?? accountingUnit.key)
						: access.loading
							? 'Wird geladen…'
							: 'Kein Haushalt verfügbar'}
				</Select.Trigger>
				<Select.Content>
					{#each accountingUnits as unit (unit.key)}
						<Select.Item value={unit.key} label={unit.name ?? unit.key} />
					{/each}
				</Select.Content>
			</Select.Root>
		</div>

		<div class="flex flex-col gap-1">
			<div class="flex h-6 items-center justify-between">
				<label for="side-nav-fiscal-year" class={labelClass}>Haushaltsjahr</label>
				{#if canEditFiscalYears}
					<Button
						variant="ghost"
						size="icon-xs"
						class={editButtonClass}
						aria-label="Haushaltsjahr bearbeiten"
						title="Haushaltsjahre verwalten"
						onclick={onEditFiscalYear}
					>
						<PencilIcon />
					</Button>
				{/if}
			</div>
			<Select.Root
				type="single"
				bind:value={() => fiscalYear?.key ?? '', onFiscalYearChange}
				disabled={fiscalYears.length === 0}
			>
				<Select.Trigger id="side-nav-fiscal-year" class={selectTriggerClass}>
					{fiscalYear
						? (fiscalYear.name ?? fiscalYear.key)
						: access.loading
							? 'Wird geladen…'
							: 'Kein Haushaltsjahr verfügbar'}
				</Select.Trigger>
				<Select.Content>
					{#each fiscalYears as year (year.key)}
						<Select.Item value={year.key} label={year.name ?? year.key} />
					{/each}
				</Select.Content>
			</Select.Root>
		</div>
	</div>

	<ul class="flex flex-col gap-0.5">
		{#each navItems as item (item.key)}
			<li>
				<button
					type="button"
					class={cn(menuItemClass, selectedPageKey === item.key && selectedMenuItemClass)}
					aria-current={selectedPageKey === item.key ? 'page' : undefined}
					onclick={() => onPageKeySelect(item.key)}
				>
					{item.label}
				</button>
			</li>
		{/each}
	</ul>

	<div class="mt-auto flex items-center gap-2 rounded-[10px] border border-sidebar-border py-3.5 pr-2 pl-3">
		<a
			href={resolve('/profil')}
			class="group flex min-w-0 flex-1 flex-col gap-1.5 rounded focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-sidebar-ring"
			aria-label={`Profil von ${auth.displayName} öffnen`}
			title="Profil öffnen"
			onclick={onNavigate}
		>
			<span class="truncate text-sm font-semibold group-hover:text-white">{auth.displayName}</span>
			<span class="flex flex-col gap-0.5">
				{#each roleNames as role (role)}
					<span class="truncate text-[13px] text-sidebar-muted-foreground" title={role}>{role}</span>
				{/each}
			</span>
		</a>
		<Button
			variant="ghost"
			size="icon"
			class="flex-none text-sidebar-muted-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
			aria-label="Abmelden"
			title="Abmelden"
			onclick={() => void auth.logout()}
		>
			<LogOutIcon />
		</Button>
	</div>
</nav>
