<script lang="ts">
	import { onMount } from 'svelte';
	import { toast } from 'svelte-sonner';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import { resolve } from '$app/paths';
	import { Badge } from '#lib/components/ui/badge/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import EmptyState from '#lib/components/EmptyState.svelte';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import apiClient from '#lib/api/client.js';
	import type { AccountingUnitListItem } from '#lib/api/types.js';
	import { confirm } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { useDataLoader, type LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import CreateAccountingUnitDialog from './CreateAccountingUnitDialog.svelte';

	const userAccess = useUserAccess();

	async function fetchAccountingUnits(): LoadFunctionResult<AccountingUnitListItem[]> {
		const { data, response } = await apiClient.GET('/api/manage-accounting-units/list-accounting-units');
		return response.ok && data ? { value: data } : { error: response };
	}

	const accountingUnitsLoader = useDataLoader(fetchAccountingUnits, 'Haushalte konnten nicht geladen werden');

	/** Changes for every opening, so the dialog starts with an empty form */
	let createId = $state(0);
	let createOpen = $state(false);

	onMount(() => void accountingUnitsLoader.load());

	// The side navigation lists the Haushalte too, so it is updated after every change
	function handleChanged() {
		void accountingUnitsLoader.load();
		void userAccess.load();
	}

	function openCreate() {
		createId++;
		createOpen = true;
	}

	function confirmDelete(accountingUnit: AccountingUnitListItem) {
		const name = accountingUnit.name ?? accountingUnit.key;
		confirm({
			title: `Haushalt „${name}“ löschen?`,
			description:
				'Alle Rollen der Benutzer in diesem Haushalt werden ebenfalls entfernt. Das kann nicht rückgängig gemacht werden.',
			confirmText: 'Löschen',
			destructive: true,
			onConfirm: async () => {
				try {
					const { response } = await apiClient.DELETE('/api/manage-accounting-units/delete-accounting-unit', {
						params: { query: { key: accountingUnit.key } }
					});

					if (response.ok) {
						toast.success(`Haushalt „${name}“ wurde gelöscht`);
						handleChanged();
					} else if (response.status === 409) {
						toast.error('Der Haushalt hat noch Haushaltsjahre und kann daher nicht gelöscht werden');
					} else if (response.status === 404) {
						toast.error('Der Haushalt existiert nicht mehr');
						handleChanged();
					} else {
						toast.error(`Haushalt konnte nicht gelöscht werden (HTTP ${response.status})`);
					}
				} catch {
					toast.error('Haushalt konnte nicht gelöscht werden');
				}
			}
		});
	}
</script>

<div class="flex flex-col gap-6">
	<div class="flex flex-wrap items-center justify-between gap-3">
		<h1 class="text-3xl font-semibold tracking-tight">Haushalte verwalten</h1>
		<Button onclick={openCreate}>
			<PlusIcon />
			Haushalt anlegen
		</Button>
	</div>

	{#if accountingUnitsLoader.error}
		<ErrorAlert message={accountingUnitsLoader.error} onRetry={() => void accountingUnitsLoader.load()} />
	{/if}

	{#if accountingUnitsLoader.initialLoading}
		<PageSpinner />
	{/if}

	{#if accountingUnitsLoader.value?.length === 0}
		<EmptyState description="Noch keine Haushalte angelegt" />
	{/if}

	{#if accountingUnitsLoader.value && accountingUnitsLoader.value.length > 0}
		<div class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4">
			{#each accountingUnitsLoader.value as accountingUnit (accountingUnit.key)}
				{@const name = accountingUnit.name ?? accountingUnit.key}
				<Card.Root class="pb-0">
					<Card.Header>
						<Card.Title class="truncate text-base">{name}</Card.Title>
						<Card.Action>
							<Badge variant="secondary">{accountingUnit.key}</Badge>
						</Card.Action>
					</Card.Header>
					<Card.Content class="flex-1">
						<p
							class={['line-clamp-3', !accountingUnit.description && 'text-muted-foreground']}
							title={accountingUnit.description ?? undefined}
						>
							{accountingUnit.description ?? 'Keine Beschreibung'}
						</p>
					</Card.Content>
					<Card.Footer class="grid grid-cols-2 divide-x border-t p-0 [.border-t]:pt-0">
						<Button
							variant="ghost"
							class="h-11 rounded-none"
							href={resolve('/haushalte-verwalten/[accountingUnitKey]', { accountingUnitKey: accountingUnit.key })}
							aria-label={`Haushalt ${name} bearbeiten`}
						>
							<PencilIcon />
							Bearbeiten
						</Button>
						<Button
							variant="ghost"
							class="h-11 rounded-none text-destructive hover:bg-destructive/10 hover:text-destructive"
							aria-label={`Haushalt ${name} löschen`}
							onclick={() => confirmDelete(accountingUnit)}
						>
							<Trash2Icon />
							Löschen
						</Button>
					</Card.Footer>
				</Card.Root>
			{/each}
		</div>
	{/if}
</div>

{#if createId > 0}
	{#key createId}
		<CreateAccountingUnitDialog bind:open={createOpen} onCreated={handleChanged} />
	{/key}
{/if}
