<script lang="ts">
	import * as Pagination from '#lib/components/ui/pagination/index.js';
	import * as Select from '#lib/components/ui/select/index.js';
	import type { OffsetPagination } from '#lib/pagination.js';

	interface Props {
		pagination: OffsetPagination;
		totalCount: number;
		pageSizes: number[];
		onChange: (pagination: OffsetPagination) => void;
	}

	let { pagination, totalCount, pageSizes, onChange }: Props = $props();
</script>

<div class="flex flex-wrap items-center justify-end gap-3">
	<Pagination.Root
		class="mx-0 w-auto"
		count={totalCount}
		perPage={pagination.pageSize}
		page={pagination.page}
		onPageChange={(page) => onChange({ ...pagination, page })}
	>
		{#snippet children({ pages, currentPage })}
			<Pagination.Content>
				<Pagination.Item>
					<Pagination.Previous />
				</Pagination.Item>
				{#each pages as page (page.key)}
					<Pagination.Item>
						{#if page.type === 'ellipsis'}
							<Pagination.Ellipsis />
						{:else}
							<Pagination.Link {page} isActive={currentPage === page.value}>{page.value}</Pagination.Link>
						{/if}
					</Pagination.Item>
				{/each}
				<Pagination.Item>
					<Pagination.Next />
				</Pagination.Item>
			</Pagination.Content>
		{/snippet}
	</Pagination.Root>

	<!-- A new page size starts on the first page, so the shown items do not jump -->
	<Select.Root
		type="single"
		value={String(pagination.pageSize)}
		onValueChange={(value) => onChange({ page: 1, pageSize: Number(value) })}
	>
		<Select.Trigger aria-label="Einträge pro Seite">{pagination.pageSize} / Seite</Select.Trigger>
		<Select.Content>
			{#each pageSizes as size (size)}
				<Select.Item value={String(size)} label={`${size} / Seite`} />
			{/each}
		</Select.Content>
	</Select.Root>
</div>
