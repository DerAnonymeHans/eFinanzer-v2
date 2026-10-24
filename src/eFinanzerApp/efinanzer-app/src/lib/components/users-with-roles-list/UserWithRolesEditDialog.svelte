<script lang="ts">
	import { SvelteSet } from 'svelte/reactivity';
	import { toast } from 'svelte-sonner';
	import { Button } from '#lib/components/ui/button/index.js';
	import { Checkbox } from '#lib/components/ui/checkbox/index.js';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import { Label } from '#lib/components/ui/label/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import UserSearchSelect from '#lib/components/UserSearchSelect.svelte';
	import type { User } from '#lib/api/types.js';
	import type { RoleKey, UserWithRoles } from './user-with-roles.js';

	interface Props {
		open: boolean;
		/** The member whose roles are edited. Without one, a new member is searched and added. */
		user?: UserWithRoles;
		title: string;
		description?: string;
		roleKeyToName: (key: RoleKey) => string;
		/** All roles, in display order */
		existingRoles: RoleKey[];
		/** Members that cannot be added again */
		excludeUsernames?: string[];
		/** Saves the roles. Returns whether anything may have changed, the dialog closes then. */
		onEdit: (user: UserWithRoles) => Promise<boolean>;
	}

	// Recreated for every opening (see UsersWithRolesList), so it starts with the current roles of the user
	let {
		open = $bindable(),
		user,
		title,
		description,
		roleKeyToName,
		existingRoles,
		excludeUsernames = [],
		onEdit
	}: Props = $props();

	// svelte-ignore state_referenced_locally
	const isNew = !user;
	// svelte-ignore state_referenced_locally
	const initialRoles = user?.roles ?? [];
	const selectedRoles = new SvelteSet<RoleKey>(initialRoles);
	let newUser = $state<User>();
	let saving = $state(false);

	const username = $derived(user?.username ?? newUser?.username);
	const changed = $derived(
		selectedRoles.size !== initialRoles.length || initialRoles.some((role) => !selectedRoles.has(role))
	);
	// A new member needs a role, otherwise nothing would be added
	const canSave = $derived(!!username && changed && (!isNew || selectedRoles.size > 0));

	function toggle(role: RoleKey, checked: boolean) {
		if (checked) selectedRoles.add(role);
		else selectedRoles.delete(role);
	}

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!username || !canSave) return;
		saving = true;
		try {
			// In display order
			const roles = existingRoles.filter((role) => selectedRoles.has(role));
			if (await onEdit({ username, roles })) open = false;
		} catch {
			toast.error('Rollen konnten nicht gespeichert werden');
		} finally {
			saving = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
			<Dialog.Header>
				<Dialog.Title>{title}</Dialog.Title>
				{#if description}
					<Dialog.Description>{description}</Dialog.Description>
				{/if}
			</Dialog.Header>

			{#if isNew}
				<UserSearchSelect
					aria-label="Benutzer suchen"
					value={newUser}
					onChange={(selectedUser) => (newUser = selectedUser)}
					{excludeUsernames}
				/>
			{/if}

			<div class="flex flex-col gap-3" role="group" aria-label="Rollen">
				{#each existingRoles as role (role)}
					{@const id = `role-${role}`}
					<div class="flex items-center gap-2">
						<Checkbox {id} checked={selectedRoles.has(role)} onCheckedChange={(checked) => toggle(role, checked)} />
						<Label for={id}>{roleKeyToName(role)}</Label>
					</div>
				{/each}
			</div>

			{#if !isNew && selectedRoles.size === 0}
				<p class="text-destructive">Ohne Rolle wird der Benutzer entfernt.</p>
			{/if}

			<Dialog.Footer>
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">Abbrechen</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" disabled={saving || !canSave}>
					{#if saving}<Spinner />{/if}
					{isNew ? 'Hinzufügen' : 'Speichern'}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
