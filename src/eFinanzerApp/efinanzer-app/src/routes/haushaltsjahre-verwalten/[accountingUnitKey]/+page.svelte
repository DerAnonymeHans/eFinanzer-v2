<script lang="ts">
	import { toast } from 'svelte-sonner';
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import type { PageProps } from './$types';
	import { Badge } from '#lib/components/ui/badge/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Table from '#lib/components/ui/table/index.js';
	import * as Tooltip from '#lib/components/ui/tooltip/index.js';
	import EmptyState from '#lib/components/EmptyState.svelte';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import apiClient from '#lib/api/client.js';
	import { useDataLoader, type LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import type { FiscalYearListItem } from '#lib/api/types.js';
	import { confirm } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { formatDate } from '#lib/texts/date-time.js';
	import FiscalYearDialog from './FiscalYearDialog.svelte';
	import { suggestNextPeriod } from './fiscal-year-validation.js';

	interface DialogState {
		/** Changes for every opening, so the dialog starts with fresh values */
		id: number;
		/** Undefined to create a new fiscal year */
		fiscalYear?: FiscalYearListItem;
	}

	function hasBookings(fiscalYear: FiscalYearListItem) {
		return Number(fiscalYear.receiptsCount) > 0 || Number(fiscalYear.budgetsCount) > 0;
	}

	let { params }: PageProps = $props();

	const userAccess = useUserAccess();
	const accountingUnitName = $derived(
		userAccess.value?.accountingUnits.find((unit) => unit.key === params.accountingUnitKey)?.name ??
			params.accountingUnitKey
	);

	async function fetchFiscalYears(accountingUnitKey: string): LoadFunctionResult<FiscalYearListItem[]> {
		const { data, response } = await apiClient.GET('/api/manage-fiscal-years/list-fiscal-years', {
			params: { query: { accountingUnitKey } }
		});
		if (response.ok && data) return { value: data };
		if (response.status === 404) return { error: `Den Haushalt „${accountingUnitKey}“ gibt es nicht` };
		return { error: response };
	}

	const fiscalYearsLoader = useDataLoader(fetchFiscalYears, 'Haushaltsjahre konnten nicht geladen werden');
	const fiscalYears = $derived(fiscalYearsLoader.value);

	let dialogState = $state<DialogState>();
	let dialogOpen = $state(false);

	// Loads again when another Haushalt is selected in the navigation
	$effect(() => {
		void fiscalYearsLoader.load(params.accountingUnitKey);
	});

	// The side navigation lists the fiscal years too, so it is updated after every change
	function handleFiscalYearsChanged() {
		void fiscalYearsLoader.load(params.accountingUnitKey);
		void userAccess.load();
	}

	function openCreateDialog(fiscalYear?: FiscalYearListItem) {
		dialogState = { id: (dialogState?.id ?? 0) + 1, fiscalYear };
		dialogOpen = true;
	}

	function confirmDelete(fiscalYear: FiscalYearListItem) {
		const name = fiscalYear.name ?? fiscalYear.key;
		const accountingUnitKey = params.accountingUnitKey;
		confirm({
			title: `Haushaltsjahr „${name}“ löschen?`,
			description: 'Das kann nicht rückgängig gemacht werden.',
			confirmText: 'Löschen',
			destructive: true,
			onConfirm: async () => {
				try {
					const { response } = await apiClient.DELETE('/api/manage-fiscal-years/delete-fiscal-year', {
						params: { query: { accountingUnitKey, key: fiscalYear.key } }
					});

					if (response.ok) {
						toast.success(`Haushaltsjahr „${name}“ wurde gelöscht`);
						handleFiscalYearsChanged();
					} else if (response.status === 409) {
						toast.error('Das Haushaltsjahr hat noch Budgets oder Belege und kann daher nicht gelöscht werden');
						void fiscalYearsLoader.load(accountingUnitKey);
					} else if (response.status === 404) {
						toast.error('Das Haushaltsjahr existiert nicht mehr');
						handleFiscalYearsChanged();
					} else {
						toast.error(`Haushaltsjahr konnte nicht gelöscht werden (HTTP ${response.status})`);
					}
				} catch {
					toast.error('Haushaltsjahr konnte nicht gelöscht werden');
				}
			}
		});
	}
</script>

<div class="flex flex-col gap-6">
	<div class="flex flex-wrap items-center justify-between gap-3">
		<div>
			<h1 class="text-3xl font-semibold tracking-tight">Haushaltsjahre verwalten</h1>
			<p class="mt-1 text-muted-foreground">Haushalt {accountingUnitName}</p>
		</div>
		<Button disabled={!fiscalYears} onclick={() => openCreateDialog()}>
			<PlusIcon />
			Haushaltsjahr anlegen
		</Button>
	</div>

	{#if fiscalYearsLoader.error}
		<ErrorAlert
			message={fiscalYearsLoader.error}
			onRetry={() => void fiscalYearsLoader.load(params.accountingUnitKey)}
		/>
	{/if}

	{#if fiscalYearsLoader.initialLoading}
		<PageSpinner />
	{/if}

	{#if fiscalYears?.length === 0}
		<EmptyState description="Noch keine Haushaltsjahre angelegt" />
	{/if}

	{#if fiscalYears && fiscalYears.length > 0}
		<div class="overflow-x-auto rounded-xl bg-card ring-1 ring-foreground/10">
			<Table.Root>
				<Table.Header>
					<Table.Row>
						<Table.Head class="pl-4">Name</Table.Head>
						<Table.Head>Zeitraum</Table.Head>
						<Table.Head class="text-right">Budgets</Table.Head>
						<Table.Head class="text-right">Belege</Table.Head>
						<Table.Head><span class="sr-only">Aktionen</span></Table.Head>
					</Table.Row>
				</Table.Header>
				<Table.Body>
					{#each fiscalYears as fiscalYear (fiscalYear.key)}
						{@const name = fiscalYear.name ?? fiscalYear.key}
						{@const deletable = !hasBookings(fiscalYear)}
						<Table.Row>
							<Table.Cell class="pl-4">
								<span class="flex flex-wrap items-center gap-2">
									<strong class="font-semibold">{name}</strong>
									{#if fiscalYear.name}
										<Badge variant="secondary">{fiscalYear.key}</Badge>
									{/if}
								</span>
							</Table.Cell>
							<Table.Cell>{formatDate(fiscalYear.startDate)} – {formatDate(fiscalYear.endDate)}</Table.Cell>
							<Table.Cell class="text-right">{fiscalYear.budgetsCount}</Table.Cell>
							<Table.Cell class="text-right">{fiscalYear.receiptsCount}</Table.Cell>
							<Table.Cell class="text-right">
								<div class="flex justify-end gap-1">
									<Button
										variant="ghost"
										size="icon"
										aria-label={`Haushaltsjahr ${name} bearbeiten`}
										title="Bearbeiten"
										onclick={() => openCreateDialog(fiscalYear)}
									>
										<PencilIcon />
									</Button>
									<Tooltip.Root>
										<!-- Only marked as disabled, a disabled button would get no pointer events and so no tooltip -->
										<Tooltip.Trigger>
											{#snippet child({ props })}
												<Button
													{...props}
													variant="ghost"
													size="icon"
													class="text-destructive hover:bg-destructive/10 hover:text-destructive aria-disabled:cursor-not-allowed aria-disabled:opacity-50 aria-disabled:hover:bg-transparent"
													aria-label={`Haushaltsjahr ${name} löschen`}
													aria-disabled={!deletable || undefined}
													onclick={() => deletable && confirmDelete(fiscalYear)}
												>
													<Trash2Icon />
												</Button>
											{/snippet}
										</Tooltip.Trigger>
										<Tooltip.Content>
											{deletable ? 'Löschen' : 'Hat noch Budgets oder Belege und kann nicht gelöscht werden'}
										</Tooltip.Content>
									</Tooltip.Root>
								</div>
							</Table.Cell>
						</Table.Row>
					{/each}
				</Table.Body>
			</Table.Root>
		</div>
	{/if}
</div>

{#if dialogState}
	{#key dialogState.id}
		<FiscalYearDialog
			bind:open={dialogOpen}
			accountingUnitKey={params.accountingUnitKey}
			fiscalYear={dialogState.fiscalYear}
			suggestedPeriod={suggestNextPeriod(fiscalYears?.[0]?.endDate)}
			onSaved={handleFiscalYearsChanged}
		/>
	{/key}
{/if}
