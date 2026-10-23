<script lang="ts" module>
	import CircleCheckIcon from '@lucide/svelte/icons/circle-check';
	import CircleXIcon from '@lucide/svelte/icons/circle-x';
	import ClockIcon from '@lucide/svelte/icons/clock';
	import type { RoleChangeRequest } from '#lib/api/types.js';

	// The status sets the color of the status pill and of the line next to the decision's comment
	const statuses = {
		pending: { label: 'Offen', icon: ClockIcon, pill: 'bg-warning', comment: 'border-warning-border' },
		approved: { label: 'Angenommen', icon: CircleCheckIcon, pill: 'bg-success', comment: 'border-success-border' },
		dismissed: { label: 'Abgelehnt', icon: CircleXIcon, pill: 'bg-destructive', comment: 'border-danger-border' }
	};

	function getStatusKey(request: RoleChangeRequest): keyof typeof statuses {
		if (request.resolution === 'Approved') return 'approved';
		if (request.resolution === 'Dismissed') return 'dismissed';
		return 'pending';
	}
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';
	import * as Card from '#lib/components/ui/card/index.js';
	import RoleChangeList from '#lib/components/RoleChangeList.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import { formatDateTime } from '#lib/texts/date-time.js';

	interface Props {
		request: RoleChangeRequest;
		/** Name of an accounting unit, the key if it is not known (anymore) */
		getAccountingUnitName: (key: string) => string;
		/** Shown in the header, e.g. to withdraw the request */
		action?: Snippet;
	}

	let { request, getAccountingUnitName, action }: Props = $props();

	const status = $derived(statuses[getStatusKey(request)]);
</script>

<!-- A comment with its label, shown as a quote so it stands out from the generated texts -->
{#snippet comment(label: string, text: string, borderClass: string)}
	<div class="flex flex-col gap-1.5">
		<span class="text-[13px] font-semibold">{label}</span>
		<p class={['border-l-3 py-0.5 pl-3 whitespace-pre-wrap', borderClass]}>{text}</p>
	</div>
{/snippet}

<Card.Root size="sm">
	<Card.Header class="border-b">
		<Card.Title class="flex flex-wrap items-center gap-2.5 font-medium">
			<span
				class={[
					'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-[13px] font-semibold text-white',
					status.pill
				]}
			>
				<status.icon class="size-3.5" />
				{status.label}
			</span>
			<span>Gestellt am {formatDateTime(request.createdAt)}</span>
		</Card.Title>
		{#if action}
			<Card.Action>{@render action()}</Card.Action>
		{/if}
	</Card.Header>
	<Card.Content class="flex flex-col gap-3.5">
		<RoleChangeList roleChanges={request.roleChanges} {getAccountingUnitName} />

		{#if request.requestComment}
			{@render comment('Begründung', request.requestComment, 'border-input')}
		{/if}

		{#if request.resolvedAt}
			<div class="flex flex-col gap-2.5 border-t pt-3.5">
				<!-- The status pill in the header already shows the decision in color -->
				<div>
					<strong class="font-semibold">{status.label}</strong>
					{#if request.resolvedBy}
						von <UserName username={request.resolvedBy} />
					{/if}
					am {formatDateTime(request.resolvedAt)}
				</div>
				{#if request.resolutionComment}
					{@render comment('Kommentar', request.resolutionComment, status.comment)}
				{/if}
			</div>
		{/if}
	</Card.Content>
</Card.Root>
