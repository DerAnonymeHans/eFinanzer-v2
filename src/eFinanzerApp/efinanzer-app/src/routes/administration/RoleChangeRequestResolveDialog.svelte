<script lang="ts" module>
	import type { components } from '#lib/api/schema.js';
	import type { RoleChangeRequestResolutionStatus } from '#lib/api/types.js';

	export type Resolution = NonNullable<RoleChangeRequestResolutionStatus>;
	type NotApplicableError = components['schemas']['RoleChangeRequestNotApplicableError'];

	/** Same limit as the API */
	const COMMENT_MAX_LENGTH = 500;

	// Both conflicts share the status code, only the one of a request that cannot be applied has a reason
	function isNotApplicableError(error: unknown): error is NotApplicableError {
		return typeof error === 'object' && error !== null && 'reason' in error;
	}
</script>

<script lang="ts">
	import { toast } from 'svelte-sonner';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import { Label } from '#lib/components/ui/label/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import CountedTextarea from '#lib/components/CountedTextarea.svelte';
	import RoleChangeList from '#lib/components/RoleChangeList.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import apiClient from '#lib/api/client.js';
	import type { RoleChangeRequest } from '#lib/api/types.js';
	import { formatDateTime } from '#lib/texts/date-time.js';

	interface Props {
		open: boolean;
		request: RoleChangeRequest;
		resolution: Resolution;
		getAccountingUnitName: (key: string) => string;
		/** Called after the request was resolved, or when the shown request turned out to be outdated */
		onChanged: () => void;
	}

	// Recreated for every opening (see RoleChangeRequestsList), so no comment of an earlier opening is left
	let { open = $bindable(), request, resolution, getAccountingUnitName, onChanged }: Props = $props();

	let comment = $state('');
	let saving = $state(false);
	const isApproval = $derived(resolution === 'Approved');

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		const failureText = isApproval ? 'Antrag konnte nicht angenommen werden' : 'Antrag konnte nicht abgelehnt werden';
		saving = true;
		try {
			const { response, error } = await apiClient.PUT('/api/manage-role-change-requests/resolve-role-change-request', {
				params: { query: { id: request.id } },
				body: { resolution, comment: comment.trim() || null }
			});

			if (response.ok) {
				toast.success(isApproval ? 'Antrag wurde angenommen' : 'Antrag wurde abgelehnt');
				onChanged();
				open = false;
			} else if (response.status === 409 && isNotApplicableError(error)) {
				toast.error(
					'Der Antrag kann nicht angewendet werden, eventuell gibt es den Benutzer oder einen Haushalt nicht mehr'
				);
			} else if (response.status === 409) {
				toast.error('Der Antrag wurde bereits entschieden');
				onChanged();
				open = false;
			} else if (response.status === 404) {
				toast.error('Den Antrag gibt es nicht mehr');
				onChanged();
				open = false;
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig');
			} else {
				// The API reverts the decision if not all roles could be changed, so the request stays open
				toast.error(`${failureText}, der Antrag bleibt offen (HTTP ${response.status})`);
			}
		} catch {
			toast.error(failureText);
		} finally {
			saving = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
			<Dialog.Header>
				<Dialog.Title>{isApproval ? 'Antrag annehmen?' : 'Antrag ablehnen?'}</Dialog.Title>
				<Dialog.Description>
					Antrag von <UserName username={request.requestedBy} />, gestellt am {formatDateTime(request.createdAt)}.
					{isApproval ? 'Die Rollen werden sofort geändert.' : 'Die Rollen bleiben unverändert.'}
				</Dialog.Description>
			</Dialog.Header>

			<RoleChangeList roleChanges={request.roleChanges} {getAccountingUnitName} />

			{#if request.requestComment}
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground">Begründung</span>
					<p class="whitespace-pre-wrap">{request.requestComment}</p>
				</div>
			{/if}

			<div class="flex flex-col gap-1.5">
				<Label for="resolve-role-change-request-comment">Kommentar (optional)</Label>
				<CountedTextarea
					id="resolve-role-change-request-comment"
					bind:value={comment}
					maxlength={COMMENT_MAX_LENGTH}
					autofocus
				/>
			</div>

			<Dialog.Footer>
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">Abbrechen</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" variant={isApproval ? 'default' : 'destructive-solid'} disabled={saving}>
					{#if saving}<Spinner />{/if}
					{isApproval ? 'Annehmen' : 'Ablehnen'}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
