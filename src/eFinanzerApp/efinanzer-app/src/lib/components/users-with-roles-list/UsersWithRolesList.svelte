<script lang="ts">
	import PencilIcon from '@lucide/svelte/icons/pencil';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import UserPlusIcon from '@lucide/svelte/icons/user-plus';
	import { Badge } from '#lib/components/ui/badge/index.js';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import * as Table from '#lib/components/ui/table/index.js';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import { confirm } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';
	import type { RoleKey, UserWithRoles } from './user-with-roles.js';
	import UserWithRolesEditDialog from './UserWithRolesEditDialog.svelte';

	interface Props {
		title: string;
		description?: string;
		/** Shown instead of the table if there are no users */
		emptyText?: string;
		showAddUser: boolean;
		showEditUser: boolean;
		showDeleteUser: boolean;
		/** Undefined while loading for the first time */
		users: UserWithRoles[] | undefined;
		/** Loading again, the shown users are dimmed meanwhile */
		loading?: boolean;
		/** Shown instead of the users, with a button calling onRetry */
		error?: string;
		onRetry?: () => void;
		roleKeyToName: (key: RoleKey) => string;
		/** All roles, in display order */
		existingRoles: RoleKey[];
		/** Title of the dialog, the user is undefined when a new one is added */
		editDialogTitle?: (userWithRoles: UserWithRoles | undefined) => string;
		/** Description of the dialog, the user is undefined when a new one is added */
		editDialogDescription?: (userWithRoles: UserWithRoles | undefined) => string;
		removeDialogTitle: (userWithRoles: UserWithRoles) => string;
		removeDialogDescription?: (userWithRoles: UserWithRoles) => string;
		/** Asks before removing a user */
		hasRemoveDialog: boolean;
		/**
		 * Applies the roles of a user, "before" has no roles for a new one. Returns whether anything may have changed,
		 * the dialog closes then.
		 */
		onEdit?: (before: UserWithRoles, after: UserWithRoles) => Promise<boolean>;
		onRemove?: (username: string) => Promise<void>;
	}

	let {
		title,
		description,
		emptyText = 'Keine Mitglieder',
		showAddUser,
		showEditUser,
		showDeleteUser,
		users,
		loading = false,
		error,
		onRetry,
		roleKeyToName,
		existingRoles,
		editDialogTitle = (user) => (user ? 'Rollen bearbeiten' : 'Mitglied hinzufügen'),
		editDialogDescription,
		removeDialogTitle,
		removeDialogDescription,
		hasRemoveDialog,
		onEdit,
		onRemove
	}: Props = $props();

	interface DialogState {
		/** Changes for every opening, so the dialog starts with the current roles instead of earlier edits */
		id: number;
		/** Without one, a new member is added */
		user?: UserWithRoles;
	}

	let dialogState = $state<DialogState>();
	let dialogOpen = $state(false);

	const showActions = $derived(showEditUser || showDeleteUser);
	const columnCount = $derived(showActions ? 3 : 2);

	function openDialog(user?: UserWithRoles) {
		dialogState = { id: (dialogState?.id ?? 0) + 1, user };
		dialogOpen = true;
	}

	/** Shortcut for removing all roles of the user in the edit dialog */
	function remove(user: UserWithRoles) {
		if (!onRemove) return;
		if (!hasRemoveDialog) {
			void onRemove(user.username);
			return;
		}

		confirm({
			title: removeDialogTitle(user),
			description: removeDialogDescription?.(user),
			confirmText: 'Entfernen',
			destructive: true,
			onConfirm: () => onRemove(user.username)
		});
	}

	async function handleEdit(after: UserWithRoles): Promise<boolean> {
		if (!onEdit) return false;
		const before = users?.find((user) => user.username === after.username) ?? { username: after.username, roles: [] };
		return onEdit(before, after);
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-base">{title}</Card.Title>
		{#if description}
			<Card.Description>{description}</Card.Description>
		{/if}
		{#if showAddUser && users}
			<Card.Action>
				<Button variant="outline" onclick={() => openDialog()}>
					<UserPlusIcon />
					Mitglied hinzufügen
				</Button>
			</Card.Action>
		{/if}
	</Card.Header>
	<Card.Content>
		{#if error}
			<ErrorAlert message={error} {onRetry} />
		{:else if !users}
			<PageSpinner />
		{:else}
			<div
				class={['relative overflow-x-auto', loading && 'pointer-events-none opacity-60']}
				aria-busy={loading}
			>
				<Table.Root>
					<Table.Header>
						<Table.Row>
							<Table.Head>Name</Table.Head>
							<Table.Head>Rollen</Table.Head>
							{#if showActions}
								<Table.Head><span class="sr-only">Aktionen</span></Table.Head>
							{/if}
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{#each users as user (user.username)}
							{@const name = userCache.getDisplayName(user.username)}
							<Table.Row>
								<Table.Cell><UserName username={user.username} /></Table.Cell>
								<Table.Cell class="whitespace-normal">
									<div class="flex flex-wrap gap-1.5">
										{#each existingRoles.filter((role) => user.roles.includes(role)) as role (role)}
											<Badge variant="secondary">{roleKeyToName(role)}</Badge>
										{/each}
									</div>
								</Table.Cell>
								{#if showActions}
									<Table.Cell class="text-right">
										<div class="flex justify-end gap-2">
											{#if showEditUser}
												<Button
													variant="outline"
													aria-label={`Rollen von ${name} bearbeiten`}
													onclick={() => openDialog(user)}
												>
													<PencilIcon />
													Bearbeiten
												</Button>
											{/if}
											{#if showDeleteUser}
												<Button variant="destructive" aria-label={`${name} entfernen`} onclick={() => remove(user)}>
													<Trash2Icon />
													Entfernen
												</Button>
											{/if}
										</div>
									</Table.Cell>
								{/if}
							</Table.Row>
						{:else}
							<Table.Row>
								<Table.Cell colspan={columnCount} class="py-10 text-center text-muted-foreground">
									{emptyText}
								</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
			</div>
		{/if}
	</Card.Content>
</Card.Root>

{#if dialogState}
	{#key dialogState.id}
		<UserWithRolesEditDialog
			bind:open={dialogOpen}
			user={dialogState.user}
			title={editDialogTitle(dialogState.user)}
			description={editDialogDescription?.(dialogState.user)}
			{roleKeyToName}
			{existingRoles}
			excludeUsernames={users?.map((user) => user.username)}
			onEdit={handleEdit}
		/>
	{/key}
{/if}
