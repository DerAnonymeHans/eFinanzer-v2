<script lang="ts" module>
	import apiClient from '#lib/api/client.js';
	import type { RoleChangeRequest } from '#lib/api/types.js';
	import type { OffsetPagination, Page } from '#lib/pagination.js';

	/** The API allows at most 50 */
	const PAGE_SIZES = [10, 20, 50];

	interface LoadResult {
		/** The pagination the result was loaded for, a newer one means the page is still loading */
		requested: OffsetPagination;
		/** Differs from the requested one if the requested page does not exist (anymore) */
		shown: OffsetPagination;
		page?: Page<RoleChangeRequest>;
		error?: string;
	}

	async function fetchPage({
		page,
		pageSize
	}: OffsetPagination): Promise<{ page?: Page<RoleChangeRequest>; error?: string }> {
		try {
			const { data, response } = await apiClient.GET(
				'/api/manage-role-change-requests/list-pending-role-change-requests',
				{ params: { query: { PageSize: pageSize, Offset: (page - 1) * pageSize } } }
			);
			if (response.ok && data) return { page: { items: data.items, totalCount: Number(data.totalCount) } };
			return { error: `Offene Rollenänderungsanträge konnten nicht geladen werden (HTTP ${response.status})` };
		} catch {
			return { error: 'Offene Rollenänderungsanträge konnten nicht geladen werden' };
		}
	}
</script>

<script lang="ts">
	import { onMount } from 'svelte';
	import CheckIcon from '@lucide/svelte/icons/check';
	import XIcon from '@lucide/svelte/icons/x';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import * as Table from '#lib/components/ui/table/index.js';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import RoleChangeList from '#lib/components/RoleChangeList.svelte';
	import TablePagination from '#lib/components/TablePagination.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import type { RoleChangeRequestAccountingUnit } from '#lib/api/types.js';
	import { getAuth } from '#lib/auth/auth.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { formatDateTime } from '#lib/texts/date-time.js';
	import ResolveRoleChangeRequestDialog, { type Resolution } from './ResolveRoleChangeRequestDialog.svelte';

	interface DialogState {
		/** Changes for every opening, so the dialog starts without the comment of an earlier opening */
		id: number;
		request: RoleChangeRequest;
		resolution: Resolution;
	}

	const auth = getAuth();
	const access = useUserAccess();

	// Raw, so the loaded result can be matched to the pagination it was loaded for by identity
	let pagination = $state.raw<OffsetPagination>({ page: 1, pageSize: PAGE_SIZES[1] });
	let loadResult = $state.raw<LoadResult>();
	let accountingUnits = $state<RoleChangeRequestAccountingUnit[]>([]);
	let dialogState = $state<DialogState>();
	let dialogOpen = $state(false);

	// Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
	let latestRequest = 0;

	async function load(requested: OffsetPagination) {
		const request = ++latestRequest;

		let shown = requested;
		let result = await fetchPage(shown);

		// After the last request of a page was resolved, the page before is shown instead of an empty one
		const lastPage = Math.max(1, Math.ceil((result.page?.totalCount ?? 0) / requested.pageSize));
		if (result.page && requested.page > lastPage) {
			shown = { ...requested, page: lastPage };
			result = await fetchPage(shown);
		}

		if (request !== latestRequest) return;
		loadResult = { requested, shown, ...result };
	}

	function changePagination(next: OffsetPagination) {
		pagination = next;
		void load(next);
	}

	onMount(() => {
		void load(pagination);

		// Only needed for the names of the accounting units, so the requests are still shown if it fails
		apiClient
			.GET('/api/manage-role-change-requests/list-accounting-units')
			.then(({ data, response }) => {
				if (response.ok && data) accountingUnits = data;
			})
			.catch(() => undefined);
	});

	const loadedPage = $derived(loadResult?.page);
	const error = $derived(loadResult?.error);
	const loading = $derived(loadResult?.requested !== pagination);
	const shownPagination = $derived(loadResult?.shown ?? pagination);

	const getAccountingUnitName = (key: string) => accountingUnits.find((unit) => unit.key === key)?.name ?? key;

	function openDialog(request: RoleChangeRequest, resolution: Resolution) {
		dialogState = { id: (dialogState?.id ?? 0) + 1, request, resolution };
		dialogOpen = true;
	}

	function handleChanged() {
		// Loads the shown page again
		changePagination({ ...shownPagination });
		// Resolving an own request changes the roles of the logged-in user
		if (dialogState?.request.requestedBy === auth.username) void access.reload();
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-base">Offene Rollenänderungsanträge</Card.Title>
	</Card.Header>
	<Card.Content class="flex flex-col gap-4">
		{#if error}
			<ErrorAlert message={error} onRetry={() => void load(pagination)} />
		{:else if !loadedPage}
			<PageSpinner />
		{:else}
			<div class={['relative overflow-x-auto', loading && 'pointer-events-none opacity-60']} aria-busy={loading}>
				<Table.Root>
					<Table.Header>
						<Table.Row>
							<Table.Head>Antragsteller</Table.Head>
							<Table.Head>Gestellt am</Table.Head>
							<Table.Head>Rollenänderungen</Table.Head>
							<Table.Head>Begründung</Table.Head>
							<Table.Head><span class="sr-only">Aktionen</span></Table.Head>
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{#each loadedPage.items as request (request.id)}
							<Table.Row>
								<Table.Cell><UserName username={request.requestedBy} /></Table.Cell>
								<Table.Cell>{formatDateTime(request.createdAt)}</Table.Cell>
								<Table.Cell class="whitespace-normal">
									<RoleChangeList roleChanges={request.roleChanges} {getAccountingUnitName} />
								</Table.Cell>
								<Table.Cell class="whitespace-normal">
									{#if request.requestComment}
										<p class="line-clamp-3 max-w-[280px] whitespace-pre-wrap" title={request.requestComment}>
											{request.requestComment}
										</p>
									{/if}
								</Table.Cell>
								<Table.Cell class="text-right">
									<div class="flex justify-end gap-2">
										<Button variant="destructive" onclick={() => openDialog(request, 'Dismissed')}>
											<XIcon />
											Ablehnen
										</Button>
										<Button onclick={() => openDialog(request, 'Approved')}>
											<CheckIcon />
											Annehmen
										</Button>
									</div>
								</Table.Cell>
							</Table.Row>
						{:else}
							<Table.Row>
								<Table.Cell colspan={5} class="py-10 text-center text-muted-foreground">
									Keine offenen Rollenänderungsanträge
								</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
			</div>

			<TablePagination
				pagination={shownPagination}
				totalCount={loadedPage.totalCount}
				pageSizes={PAGE_SIZES}
				onChange={changePagination}
			/>
		{/if}
	</Card.Content>
</Card.Root>

{#if dialogState}
	{#key dialogState.id}
		<ResolveRoleChangeRequestDialog
			bind:open={dialogOpen}
			request={dialogState.request}
			resolution={dialogState.resolution}
			{getAccountingUnitName}
			onChanged={handleChanged}
		/>
	{/key}
{/if}
