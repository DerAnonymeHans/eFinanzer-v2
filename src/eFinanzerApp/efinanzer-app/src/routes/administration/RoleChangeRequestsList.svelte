<script lang="ts" module>
	import apiClient from '#lib/api/client.js';
	import { useDataLoader, type LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import type { RoleChangeRequest } from '#lib/api/types.js';
	import type { OffsetPagination, Page } from '#lib/components/pagination/pagination.js';

	/** The API allows at most 50 */
	const PAGE_SIZES = [10, 20, 50];

	interface LoadedPage {
		/** Differs from the requested pagination if the requested page does not exist (anymore) */
		shown: OffsetPagination;
		page: Page<RoleChangeRequest>;
	}

	async function fetchPage({ page, pageSize }: OffsetPagination): LoadFunctionResult<Page<RoleChangeRequest>> {
		const { data, response } = await apiClient.GET(
			'/api/manage-role-change-requests/list-pending-role-change-requests',
			{ params: { query: { PageSize: pageSize, Offset: (page - 1) * pageSize } } }
		);
		if (response.ok && data) return { value: { items: data.items, totalCount: Number(data.totalCount) } };
		return { error: response };
	}

	// After the last request of a page was resolved, the page before is shown instead of an empty one
	async function fetchShownPage(requested: OffsetPagination): LoadFunctionResult<LoadedPage> {
		const result = await fetchPage(requested);
		if (!('value' in result)) return result;

		const lastPage = Math.max(1, Math.ceil(result.value.totalCount / requested.pageSize));
		if (requested.page <= lastPage) return { value: { shown: requested, page: result.value } };

		const shown = { ...requested, page: lastPage };
		const lastPageResult = await fetchPage(shown);
		return 'value' in lastPageResult ? { value: { shown, page: lastPageResult.value } } : lastPageResult;
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
	import TablePagination from '#lib/components/pagination/TablePagination.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import type { RoleChangeRequestAccountingUnit } from '#lib/api/types.js';
	import { getAuth } from '#lib/auth/auth.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { formatDateTime } from '#lib/texts/date-time.js';
	import RoleChangeRequestResolveDialog, { type Resolution } from './RoleChangeRequestResolveDialog.svelte';

	interface DialogState {
		/** Changes for every opening, so the dialog starts without the comment of an earlier opening */
		id: number;
		request: RoleChangeRequest;
		resolution: Resolution;
	}

	const auth = getAuth();
	const userAccess = useUserAccess();

	let pagination = $state.raw<OffsetPagination>({ page: 1, pageSize: PAGE_SIZES[1] });
	const pageLoader = useDataLoader(fetchShownPage, 'Offene Rollenänderungsanträge konnten nicht geladen werden');
	let accountingUnits = $state<RoleChangeRequestAccountingUnit[]>([]);
	let dialogState = $state<DialogState>();
	let dialogOpen = $state(false);

	function changePagination(next: OffsetPagination) {
		pagination = next;
		void pageLoader.load(next);
	}

	onMount(() => {
		void pageLoader.load(pagination);

		// Only needed for the names of the accounting units, so the requests are still shown if it fails
		apiClient
			.GET('/api/manage-role-change-requests/list-accounting-units')
			.then(({ data, response }) => {
				if (response.ok && data) accountingUnits = data;
			})
			.catch(() => undefined);
	});

	const loadedPage = $derived(pageLoader.value?.page);
	const error = $derived(pageLoader.error);
	const loading = $derived(pageLoader.loading);
	const shownPagination = $derived(pageLoader.value?.shown ?? pagination);

	const getAccountingUnitName = (key: string) => accountingUnits.find((unit) => unit.key === key)?.name ?? key;

	function openDialog(request: RoleChangeRequest, resolution: Resolution) {
		dialogState = { id: (dialogState?.id ?? 0) + 1, request, resolution };
		dialogOpen = true;
	}

	function handleChanged() {
		// Loads the shown page again
		changePagination({ ...shownPagination });
		// Resolving an own request changes the roles of the logged-in user
		if (dialogState?.request.requestedBy === auth.username) void userAccess.load();
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-base">Offene Rollenänderungsanträge</Card.Title>
	</Card.Header>
	<Card.Content class="flex flex-col gap-4">
		{#if error}
			<ErrorAlert message={error} onRetry={() => void pageLoader.load(pagination)} />
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
		<RoleChangeRequestResolveDialog
			bind:open={dialogOpen}
			request={dialogState.request}
			resolution={dialogState.resolution}
			{getAccountingUnitName}
			onChanged={handleChanged}
		/>
	{/key}
{/if}
