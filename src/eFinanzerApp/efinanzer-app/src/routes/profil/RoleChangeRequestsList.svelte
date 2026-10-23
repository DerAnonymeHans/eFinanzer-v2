<script lang="ts">
	import { onMount } from 'svelte';
	import { SvelteMap } from 'svelte/reactivity';
	import { toast } from 'svelte-sonner';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import EmptyState from '#lib/components/EmptyState.svelte';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import apiClient from '#lib/api/client.js';
	import { useDataLoader, type LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import type {
		AccountingUnitRole,
		RoleChangeRequest,
		RoleChangeRequestAccountingUnit
	} from '#lib/api/types.js';
	import { confirm } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
	import { mobile } from '#lib/reactivity/is-mobile.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import RoleChangeRequestsListItem from './RoleChangeRequestsListItem.svelte';
	import RoleChangeRequestsCreateDialog from './RoleChangeRequestsCreateDialog.svelte';

	interface LoadedData {
		/** Newest first */
		existingRequests: RoleChangeRequest[];
		accountingUnits: RoleChangeRequestAccountingUnit[];
	}

	const userAccess = useUserAccess();

	async function fetchData(): LoadFunctionResult<LoadedData> {
		const [roleChangeRequests, accountingUnits] = await Promise.all([
			apiClient.GET('/api/manage-role-change-requests/list-own-role-change-requests'),
			apiClient.GET('/api/manage-role-change-requests/list-accounting-units')
		]);
		if (!roleChangeRequests.response.ok || !roleChangeRequests.data) return { error: roleChangeRequests.response };
		if (!accountingUnits.response.ok || !accountingUnits.data) return { error: accountingUnits.response };
		return { value: { existingRequests: roleChangeRequests.data, accountingUnits: accountingUnits.data } };
	}

	const roleChangeRequestsLoader = useDataLoader(fetchData, 'Rollenänderungsanträge konnten nicht geladen werden');
	const data = $derived(roleChangeRequestsLoader.value);

	let createRoleChangeRequestDialogId = $state(0);
	let createRoleChangeRequestDialogOpen = $state(false);

	onMount(() => void roleChangeRequestsLoader.load());

	const currentRoles = $derived(
		new SvelteMap<string, AccountingUnitRole[]>(
			userAccess.value?.accountingUnits.map((unit) => [unit.key, unit.accountingUnitRoles]) ?? []
		)
	);

	const getAccountingUnitName = (key: string) =>
		data?.accountingUnits.find((unit) => unit.key === key)?.name ?? key;

	const pendingRequest = $derived(data?.existingRequests.find((request) => !request.resolution));
	const resolvedRequests = $derived(data?.existingRequests.filter((request) => request.resolution) ?? []);

	function openDialog() {
		createRoleChangeRequestDialogId++;
		createRoleChangeRequestDialogOpen = true;
	}

	function confirmWithdraw(request: RoleChangeRequest) {
		confirm({
			title: 'Antrag zurückziehen?',
			description: 'Der offene Rollenänderungsantrag wird gelöscht.',
			confirmText: 'Zurückziehen',
			destructive: true,
			onConfirm: async () => {
				try {
					const { response } = await apiClient.DELETE(
						'/api/manage-role-change-requests/delete-role-change-request',
						{ params: { query: { id: request.id } } }
					);

					if (response.ok) {
						toast.success('Rollenänderungsantrag wurde zurückgezogen');
					} else if (response.status === 409) {
						toast.error('Der Antrag wurde bereits entschieden');
					} else if (response.status === 404) {
						toast.error('Den Antrag gibt es nicht mehr');
					} else {
						toast.error(`Antrag konnte nicht zurückgezogen werden (HTTP ${response.status})`);
						return;
					}
					void roleChangeRequestsLoader.load();
				} catch {
					toast.error('Antrag konnte nicht zurückgezogen werden');
				}
			}
		});
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-base">Rollenänderungsanträge</Card.Title>
		{#if data && !pendingRequest}
			<Card.Action>
				<Button
					onclick={openDialog}
					aria-label={mobile.current ? 'Neuer Rollenänderungsantrag' : undefined}
					title={mobile.current ? 'Neuer Rollenänderungsantrag' : undefined}
					size={mobile.current ? 'icon' : 'default'}
				>
					<PlusIcon />
					{#if !mobile.current}Neuer Rollenänderungsantrag{/if}
				</Button>
			</Card.Action>
		{/if}
	</Card.Header>
	<Card.Content class="flex flex-col gap-3">
		{#if roleChangeRequestsLoader.error}
			<ErrorAlert message={roleChangeRequestsLoader.error} onRetry={() => void roleChangeRequestsLoader.load()} />
		{/if}

		{#if roleChangeRequestsLoader.initialLoading}
			<PageSpinner class="py-6" />
		{/if}

		{#if pendingRequest}
			<RoleChangeRequestsListItem request={pendingRequest} {getAccountingUnitName}>
				{#snippet action()}
					<Button
						variant="ghost"
						class="text-destructive hover:bg-destructive/10 hover:text-destructive"
						size={mobile.current ? 'icon' : 'default'}
						aria-label={mobile.current ? 'Zurückziehen' : undefined}
						title={mobile.current ? 'Zurückziehen' : undefined}
						onclick={() => confirmWithdraw(pendingRequest)}
					>
						<Trash2Icon />
						{#if !mobile.current}Zurückziehen{/if}
					</Button>
				{/snippet}
			</RoleChangeRequestsListItem>
		{/if}

		{#if data && data.existingRequests.length === 0}
			<EmptyState description="Noch keine Rollenänderungsanträge gestellt" />
		{/if}

		{#if resolvedRequests.length > 0}
			<h3 class="mt-3 font-semibold">Frühere Anträge</h3>
			{#each resolvedRequests as request (request.id)}
				<RoleChangeRequestsListItem {request} {getAccountingUnitName} />
			{/each}
		{/if}
	</Card.Content>
</Card.Root>

{#if data && createRoleChangeRequestDialogId > 0}
	{#key createRoleChangeRequestDialogId}
		<RoleChangeRequestsCreateDialog
			bind:open={createRoleChangeRequestDialogOpen}
			accountingUnits={data.accountingUnits}
			{currentRoles}
			onChanged={() => void roleChangeRequestsLoader.load()}
		/>
	{/key}
{/if}
