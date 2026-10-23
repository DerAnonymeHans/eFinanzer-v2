<script lang="ts" module>
	import type { AccountingUnitRole } from '#lib/api/types.js';
	import { accountingUnitRoleNames } from '#lib/texts/roles.js';

	/** Same limit as the API */
	const COMMENT_MAX_LENGTH = 500;

	const roles = Object.keys(accountingUnitRoleNames) as AccountingUnitRole[];

	const roleKey = (accountingUnitKey: string, role: AccountingUnitRole) => `${accountingUnitKey}/${role}`;
</script>

<script lang="ts">
	import { SvelteSet } from 'svelte/reactivity';
	import { toast } from 'svelte-sonner';
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import * as Alert from '#lib/components/ui/alert/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import { Checkbox } from '#lib/components/ui/checkbox/index.js';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import { Label } from '#lib/components/ui/label/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import * as Table from '#lib/components/ui/table/index.js';
	import CountedTextarea from '#lib/components/CountedTextarea.svelte';
	import apiClient from '#lib/api/client.js';
	import type { RoleChange, RoleChangeRequestAccountingUnit } from '#lib/api/types.js';

	interface Props {
		open: boolean;
		accountingUnits: RoleChangeRequestAccountingUnit[];
		/** Roles the user currently has, per accounting unit key */
		currentRoles: Map<string, AccountingUnitRole[]>;
		/** Called after the request was created, or when the shown data turned out to be outdated */
		onChanged: () => void;
	}

	// Recreated for every opening (see RoleChangeRequestsList), so no selection of an earlier opening is left
	let { open = $bindable(), accountingUnits, currentRoles, onChanged }: Props = $props();

	const added = new SvelteSet<string>();
	const removed = new SvelteSet<string>();
	let comment = $state('');
	let showEmptyError = $state(false);
	let saving = $state(false);

	const hasRole = (accountingUnitKey: string, role: AccountingUnitRole) =>
		currentRoles.get(accountingUnitKey)?.includes(role) ?? false;

	function toggle(selection: SvelteSet<string>, key: string) {
		showEmptyError = false;
		if (selection.has(key)) selection.delete(key);
		else selection.add(key);
	}

	// Ordered like the tables, so the request reads the same way as it was entered
	const getRoleChanges = (): RoleChange[] =>
		accountingUnits.flatMap((unit) =>
			roles.flatMap((role): RoleChange[] => {
				const key = roleKey(unit.key, role);
				if (added.has(key)) return [{ accountingUnitKey: unit.key, accountingUnitRole: role, isAdded: true }];
				if (removed.has(key)) return [{ accountingUnitKey: unit.key, accountingUnitRole: role, isAdded: false }];
				return [];
			})
		);

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		const roleChanges = getRoleChanges();
		if (roleChanges.length === 0) {
			showEmptyError = true;
			return;
		}

		const failureText = 'Rollenänderungsantrag konnte nicht gestellt werden';
		saving = true;
		try {
			const { response } = await apiClient.POST('/api/manage-role-change-requests/create-role-change-request', {
				body: { comment: comment.trim() || null, roleChanges }
			});

			if (response.ok) {
				toast.success('Rollenänderungsantrag wurde gestellt');
				onChanged();
				open = false;
			} else if (response.status === 409) {
				toast.error('Es gibt bereits einen offenen Rollenänderungsantrag');
				onChanged();
				open = false;
			} else if (response.status === 404) {
				toast.error('Einen der Haushalte gibt es nicht mehr');
				onChanged();
				open = false;
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig');
			} else {
				toast.error(`${failureText} (HTTP ${response.status})`);
			}
		} catch {
			toast.error(failureText);
		} finally {
			saving = false;
		}
	}
</script>

<!-- One row per accounting unit, one checkbox per role -->
{#snippet roleTable(
	caption: string,
	selection: SvelteSet<string>,
	isSelectable: (accountingUnitKey: string, role: AccountingUnitRole) => boolean,
	notSelectableReason: string,
	notSelectableChecked: boolean
)}
	<div class="max-h-[260px] overflow-auto rounded-md border">
		<Table.Root>
			<Table.Header class="sticky top-0 bg-popover">
				<Table.Row>
					<Table.Head>Haushalt</Table.Head>
					{#each roles as role (role)}
						<Table.Head class="text-center">{accountingUnitRoleNames[role]}</Table.Head>
					{/each}
				</Table.Row>
			</Table.Header>
			<Table.Body>
				{#each accountingUnits as unit (unit.key)}
					{@const unitName = unit.name ?? unit.key}
					<Table.Row>
						<Table.Cell>{unitName}</Table.Cell>
						{#each roles as role (role)}
							{@const key = roleKey(unit.key, role)}
							{@const selectable = isSelectable(unit.key, role)}
							<Table.Cell class="justify-items-center">
								<Checkbox
									aria-label={`${caption}: ${unitName} · ${accountingUnitRoleNames[role]}`}
									title={selectable ? undefined : notSelectableReason}
									disabled={!selectable}
									checked={selectable ? selection.has(key) : notSelectableChecked}
									onCheckedChange={() => toggle(selection, key)}
								/>
							</Table.Cell>
						{/each}
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	</div>
{/snippet}

<Dialog.Root bind:open>
	<Dialog.Content class="max-h-[calc(100vh-2rem)] overflow-y-auto sm:max-w-[760px]">
		<form class="flex flex-col gap-5" onsubmit={handleSubmit}>
			<Dialog.Header>
				<Dialog.Title>Neuer Rollenänderungsantrag</Dialog.Title>
				<Dialog.Description>
					Rollen auswählen, die hinzukommen oder wegfallen sollen. Der Antrag wird anschließend von der
					Finanzverwaltung geprüft.
				</Dialog.Description>
			</Dialog.Header>

			<section class="flex flex-col gap-2">
				<h3 class="font-semibold">Rollen beantragen</h3>
				{@render roleTable('Beantragen', added, (unitKey, role) => !hasRole(unitKey, role), 'Rolle bereits vorhanden', true)}
			</section>

			<section class="flex flex-col gap-2">
				<h3 class="font-semibold">Rollen abgeben</h3>
				{@render roleTable('Abgeben', removed, hasRole, 'Rolle nicht vorhanden', false)}
			</section>

			<section class="flex flex-col gap-2">
				<Label for="role-change-request-comment" class="font-semibold">Begründung (optional)</Label>
				<CountedTextarea id="role-change-request-comment" bind:value={comment} maxlength={COMMENT_MAX_LENGTH} />
			</section>

			{#if showEmptyError}
				<Alert.Root variant="destructive">
					<CircleAlertIcon />
					<Alert.Title>Mindestens eine Rolle auswählen</Alert.Title>
				</Alert.Root>
			{/if}

			<Dialog.Footer>
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">Abbrechen</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" disabled={saving}>
					{#if saving}<Spinner />{/if}
					Antrag stellen
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
