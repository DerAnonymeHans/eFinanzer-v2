<script lang="ts">
	import MinusIcon from '@lucide/svelte/icons/minus';
	import PlusIcon from '@lucide/svelte/icons/plus';
	import { Badge } from '#lib/components/ui/badge/index.js';
	import type { RoleChange } from '#lib/api/types.js';
	import { formatAccountingUnitRole } from '#lib/texts/roles.js';

	interface Props {
		roleChanges: RoleChange[];
		/** Name of an accounting unit, the key if it is not known (anymore) */
		getAccountingUnitName: (key: string) => string;
	}

	let { roleChanges, getAccountingUnitName }: Props = $props();

	const groups = $derived(
		[
			{ label: 'Beantragte Rollen', added: true, changes: roleChanges.filter((change) => change.isAdded) },
			{ label: 'Abzugebende Rollen', added: false, changes: roleChanges.filter((change) => !change.isAdded) }
		].filter((group) => group.changes.length > 0)
	);
</script>

<!-- The roles a role change request adds and removes, each group only if it has roles -->
<div class="flex flex-col gap-3">
	{#each groups as group (group.label)}
		<div class="flex flex-col gap-1.5">
			<span class="text-[13px] font-semibold">{group.label}</span>
			<div class="flex flex-wrap gap-1.5">
				{#each group.changes as change (`${change.accountingUnitKey}/${change.accountingUnitRole}`)}
					<Badge variant="outline" class="h-auto rounded-full px-2.5 py-0.5 text-[13px] font-medium">
						<!-- Only the + and − are colored, so the direction is visible at a glance -->
						{#if group.added}
							<PlusIcon class="text-success" />
						{:else}
							<MinusIcon class="text-destructive" />
						{/if}
						{formatAccountingUnitRole(getAccountingUnitName(change.accountingUnitKey), change.accountingUnitRole)}
					</Badge>
				{/each}
			</div>
		</div>
	{/each}
</div>
