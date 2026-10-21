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
	import type {
		AccountingUnitRole,
		RoleChangeRequest,
		RoleChangeRequestAccountingUnit
	} from '#lib/api/types.js';
	import { confirm } from '#lib/confirm.svelte.js';
	import { mobile } from '#lib/hooks/is-mobile.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import RoleChangeRequestCard from './RoleChangeRequestCard.svelte';
	import RoleChangeRequestDialog from './RoleChangeRequestDialog.svelte';

	interface LoadedData {
		/** Newest first */
		requests: RoleChangeRequest[];
		accountingUnits: RoleChangeRequestAccountingUnit[];
	}

	const access = useUserAccess();

	let data = $state<LoadedData>();
	let error = $state<string>();
	/** Changes for every opening, so the dialog starts without the selection of an earlier opening */
	let dialogId = $state(0);
	let dialogOpen = $state(false);

	// Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
	let latestRequest = 0;

	async function load() {
		const request = ++latestRequest;

		let result: LoadedData | undefined;
		let failure: string | undefined;
		try {
			const [requests, accountingUnits] = await Promise.all([
				apiClient.GET('/api/manage-role-change-requests/list-own-role-change-requests'),
				apiClient.GET('/api/manage-role-change-requests/list-accounting-units')
			]);
			if (requests.response.ok && requests.data && accountingUnits.response.ok && accountingUnits.data) {
				result = { requests: requests.data, accountingUnits: accountingUnits.data };
			} else {
				const status = requests.response.ok ? accountingUnits.response.status : requests.response.status;
				failure = `Rollenänderungsanträge konnten nicht geladen werden (HTTP ${status})`;
			}
		} catch {
			failure = 'Rollenänderungsanträge konnten nicht geladen werden';
		}

		if (request !== latestRequest) return;
		data = result;
		error = failure;
	}

	onMount(() => void load());

	const currentRoles = $derived(
		new SvelteMap<string, AccountingUnitRole[]>(
			access.userAccess?.accountingUnits.map((unit) => [unit.key, unit.accountingUnitRoles]) ?? []
		)
	);

	// Requests can name accounting units the user has no role in, so the names come from the list of all of them
	const getAccountingUnitName = (key: string) =>
		data?.accountingUnits.find((unit) => unit.key === key)?.name ?? key;

	const pendingRequest = $derived(data?.requests.find((request) => !request.resolution));
	const resolvedRequests = $derived(data?.requests.filter((request) => request.resolution) ?? []);

	function openDialog() {
		dialogId++;
		dialogOpen = true;
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
					void load();
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
				<!-- The text does not fit into the header on small screens, so only the icon is shown there -->
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
		{#if error}
			<ErrorAlert message={error} onRetry={() => void load()} />
		{/if}

		{#if !data && !error}
			<PageSpinner class="py-6" />
		{/if}

		{#if pendingRequest}
			<RoleChangeRequestCard request={pendingRequest} {getAccountingUnitName}>
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
			</RoleChangeRequestCard>
		{/if}

		{#if data && data.requests.length === 0}
			<EmptyState description="Noch keine Rollenänderungsanträge gestellt" />
		{/if}

		{#if resolvedRequests.length > 0}
			<h3 class="mt-3 font-semibold">Frühere Anträge</h3>
			{#each resolvedRequests as request (request.id)}
				<RoleChangeRequestCard {request} {getAccountingUnitName} />
			{/each}
		{/if}
	</Card.Content>
</Card.Root>

{#if data && dialogId > 0}
	{#key dialogId}
		<RoleChangeRequestDialog
			bind:open={dialogOpen}
			accountingUnits={data.accountingUnits}
			{currentRoles}
			onChanged={() => void load()}
		/>
	{/key}
{/if}
