<script lang="ts" module>
	import apiClient from '#lib/api/client.js';
	import type { LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import type { AdministrationRole, AdministrationUser } from '#lib/api/types.js';
	import { administrationRoleNames } from '#lib/texts/roles.js';

	/** In display order, most important first */
	const roles = Object.keys(administrationRoleNames) as AdministrationRole[];

	async function fetchAdministrationUsers(): LoadFunctionResult<AdministrationUser[]> {
		const { data, response } = await apiClient.GET('/api/manage-administration-roles/get-administration-users');
		return response.ok && data ? { value: data.administrationUsers } : { error: response };
	}
</script>

<script lang="ts">
	import { onMount } from 'svelte';
	import { toast } from 'svelte-sonner';
	import { useDataLoader } from '#lib/api/data-loading.svelte.js';
	import { getAuth } from '#lib/auth/auth.svelte.js';
	import UsersWithRolesList from '#lib/components/users-with-roles-list/UsersWithRolesList.svelte';
	import type { UserWithRoles } from '#lib/components/users-with-roles-list/user-with-roles.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';

	const auth = getAuth();
	const userAccess = useUserAccess();
	const usersLoader = useDataLoader(fetchAdministrationUsers, 'Mitglieder der Administration konnten nicht geladen werden');

	onMount(() => {
		void usersLoader.load();
	});

	// The API checks it again when saving
	const canEdit = $derived(!!userAccess.value?.manageAdministrationRoles);

	function handleChanged(username: string) {
		void usersLoader.load();
		// Changing the own roles changes what the logged-in user may access
		if (username === auth.username) void userAccess.load();
	}

	/**
	 * Sets the roles of the user, an empty list removes them from the administration.
	 * Returns whether the roles may have changed.
	 */
	async function updateRoles(
		username: string,
		administrationRoles: AdministrationRole[],
		texts: { success: string; partialFailure: string; failure: string }
	): Promise<boolean> {
		try {
			const { response } = await apiClient.PUT('/api/manage-administration-roles/update-administration-roles', {
				body: { username, administrationRoles }
			});

			if (response.ok) {
				toast.success(texts.success);
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig, eventuell gibt es den Benutzer nicht mehr');
				return false;
			} else {
				// Role changes are applied one after another, so some of them may have been saved
				toast.error(`${texts.partialFailure} (HTTP ${response.status})`);
			}
			handleChanged(username);
			return true;
		} catch {
			toast.error(texts.failure);
			return false;
		}
	}

	async function handleEdit(before: UserWithRoles, after: UserWithRoles) {
		// In display order, and only roles the API knows
		const administrationRoles = roles.filter((role) => after.roles.includes(role));
		const isNew = before.roles.length === 0;
		return updateRoles(after.username, administrationRoles, {
			success: isNew ? 'Mitglied wurde hinzugefügt' : 'Rollen wurden gespeichert',
			partialFailure: 'Rollen konnten nicht vollständig gespeichert werden',
			failure: 'Rollen konnten nicht gespeichert werden'
		});
	}

	async function handleRemove(username: string) {
		await updateRoles(username, [], {
			success: `${userCache.getDisplayName(username)} wurde aus der Administration entfernt`,
			partialFailure: 'Rollen konnten nicht vollständig entfernt werden',
			failure: 'Rollen konnten nicht entfernt werden'
		});
	}
</script>

<UsersWithRolesList
	title="Mitglieder der Administration"
	showAddUser={canEdit}
	showEditUser={canEdit}
	showDeleteUser={canEdit}
	users={usersLoader.value}
	loading={usersLoader.loading}
	error={usersLoader.error}
	onRetry={() => void usersLoader.load()}
	roleKeyToName={(key) => administrationRoleNames[key as AdministrationRole]}
	existingRoles={roles}
	emptyText="Keine Mitglieder in der Administration"
	editDialogDescription={(user) =>
		user
			? `Rollen von ${userCache.getDisplayName(user.username)} in der Administration. Die Änderungen gelten sofort.`
			: 'Person suchen und ihre Rollen in der Administration auswählen. Die Änderungen gelten sofort.'}
	hasRemoveDialog={true}
	removeDialogTitle={(user) => `${userCache.getDisplayName(user.username)} aus der Administration entfernen?`}
	removeDialogDescription={() => 'Alle Rollen in der Administration werden sofort entfernt.'}
	onEdit={handleEdit}
	onRemove={handleRemove}
/>
