<script lang="ts" module>
	import type { AccountingUnitRole } from '#lib/api/types.js';
	import { accountingUnitRoleNames } from '#lib/texts/roles.js';

	/** In display order, most important first */
	const roles = Object.keys(accountingUnitRoleNames) as AccountingUnitRole[];
</script>

<script lang="ts">
	import { toast } from 'svelte-sonner';
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import SaveIcon from '@lucide/svelte/icons/save';
	import { resolve } from '$app/paths';
	import type { PageProps } from './$types';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import * as Field from '#lib/components/ui/field/index.js';
	import { Input } from '#lib/components/ui/input/index.js';
	import CountedTextarea from '#lib/components/CountedTextarea.svelte';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import UsersWithRolesList from '#lib/components/users-with-roles-list/UsersWithRolesList.svelte';
	import type { UserWithRoles } from '#lib/components/users-with-roles-list/user-with-roles.js';
	import apiClient from '#lib/api/client.js';
	import { useDataLoader, type LoadFunctionResult } from '#lib/api/data-loading.svelte.js';
	import type { AccountingUnitDetails } from '#lib/api/types.js';
	import { confirm } from '#lib/components/confirm-dialog/confirmDialog.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';
	import { emptyToNull, toFieldErrors } from '#lib/utils.js';
	import { validateMaxLength } from '#lib/texts/validation.js';
	import { DESCRIPTION_MAX_LENGTH, NAME_MAX_LENGTH } from '../accounting-unit-validation.js';

	let { params }: PageProps = $props();

	const userAccess = useUserAccess();

	async function fetchAccountingUnit(key: string): LoadFunctionResult<AccountingUnitDetails> {
		const { data, response } = await apiClient.GET('/api/manage-accounting-units/get-accounting-unit', {
			params: { query: { key } }
		});
		if (response.ok && data) return { value: data };
		if (response.status === 404) return { error: `Den Haushalt „${key}“ gibt es nicht` };
		return { error: response };
	}

	const accountingUnitLoader = useDataLoader(fetchAccountingUnit, 'Haushalt konnte nicht geladen werden');
	const accountingUnit = $derived(accountingUnitLoader.value);

	let name = $state('');
	let description = $state('');
	/** The edited users, only saved together with the general info */
	let users = $state<UserWithRoles[]>([]);
	let submitted = $state(false);

	const savedUsers = $derived(
		(accountingUnit?.usersWithRoles ?? []).map((user) => ({ username: user.userName, roles: user.roles }))
	);
	const removedUsers = $derived(
		savedUsers.filter((saved) => !users.some((user) => user.username === saved.username))
	);

	// Loads the Haushalt and fills the form with its values, unless a newer load was started meanwhile
	async function load(key: string) {
		if (!(await accountingUnitLoader.load(key))) return;
		const result = accountingUnitLoader.value;
		name = result?.name ?? '';
		description = result?.description ?? '';
		users = (result?.usersWithRoles ?? []).map((user) => ({ username: user.userName, roles: [...user.roles] }));
	}

	// Loads again when another Haushalt is opened
	$effect(() => {
		void load(params.accountingUnitKey);
	});

	const errors = $derived({
		name: validateMaxLength(name, NAME_MAX_LENGTH),
		description: validateMaxLength(description, DESCRIPTION_MAX_LENGTH)
	});
	const shownErrors = $derived<Partial<typeof errors>>(submitted ? errors : {});

	// Users without roles are removed, the dialog says so
	async function handleEdit(_before: UserWithRoles, after: UserWithRoles) {
		const index = users.findIndex((user) => user.username === after.username);
		if (after.roles.length === 0) users = users.filter((user) => user.username !== after.username);
		else if (index === -1) users.push(after);
		else users[index] = after;
		return true;
	}

	async function handleRemove(username: string) {
		users = users.filter((user) => user.username !== username);
	}

	async function save() {
		try {
			const { response } = await apiClient.PUT('/api/manage-accounting-units/update-accounting-unit', {
				params: { query: { key: params.accountingUnitKey } },
				body: {
					name: emptyToNull(name),
					description: emptyToNull(description),
					usersWithRoles: users.map((user) => ({
						userName: user.username,
						roles: [...user.roles] as AccountingUnitRole[]
					}))
				}
			});

			if (response.ok) {
				toast.success('Änderungen wurden gespeichert');
				void load(params.accountingUnitKey);
				// The side navigation shows the name of the Haushalt and the roles of the logged-in user
				void userAccess.load();
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig, eventuell gibt es einen Benutzer nicht');
			} else if (response.status === 404) {
				toast.error('Der Haushalt existiert nicht mehr');
			} else {
				// Role changes are applied one after another, so some of them may have been saved
				toast.error(`Änderungen konnten nicht vollständig gespeichert werden (HTTP ${response.status})`);
				void load(params.accountingUnitKey);
			}
		} catch {
			toast.error('Änderungen konnten nicht gespeichert werden');
		}
	}

	function confirmSave() {
		submitted = true;
		if (Object.values(errors).some(Boolean)) return;

		confirm({
			title: 'Änderungen speichern?',
			description: saveDescription,
			confirmText: 'Speichern',
			onConfirm: save
		});
	}
</script>

{#snippet saveDescription()}
	<p>Die allgemeinen Angaben und die Rollen der Benutzer werden übernommen.</p>
	{#if removedUsers.length > 0}
		<p class="mt-2">
			Aus dem Haushalt entfernt werden:
			{#each removedUsers as user, index (user.username)}
				{#if index > 0},
				{/if}<UserName username={user.username} />
			{/each}
		</p>
	{/if}
{/snippet}

<div class="flex flex-col gap-6">
	<a
		href={resolve('/haushalte-verwalten')}
		class="inline-flex items-center gap-1.5 self-start font-medium text-primary hover:underline"
	>
		<ArrowLeftIcon class="size-4" />
		Alle Haushalte
	</a>

	{#if accountingUnitLoader.error}
		<ErrorAlert message={accountingUnitLoader.error} onRetry={() => void load(params.accountingUnitKey)} />
	{:else if !accountingUnit}
		<PageSpinner />
	{:else}
		<div class="flex flex-wrap items-center justify-between gap-3">
			<h1 class="text-3xl font-semibold tracking-tight">{accountingUnit.name ?? accountingUnit.key}</h1>
			<Button onclick={confirmSave}>
				<SaveIcon />
				Speichern
			</Button>
		</div>

		<Card.Root>
			<Card.Header>
				<Card.Title class="text-base">Allgemein</Card.Title>
			</Card.Header>
			<Card.Content>
				<Field.Group class="max-w-[560px]">
					<div class="grid gap-4 sm:grid-cols-2">
						<Field.Field>
							<Field.Label for="accounting-unit-key">Schlüssel</Field.Label>
							<Input id="accounting-unit-key" value={accountingUnit.key} disabled />
						</Field.Field>
						<Field.Field data-invalid={!!shownErrors.name || undefined}>
							<Field.Label for="accounting-unit-name">Name</Field.Label>
							<Input
								id="accounting-unit-name"
								bind:value={name}
								maxlength={NAME_MAX_LENGTH}
								aria-invalid={!!shownErrors.name || undefined}
							/>
							<Field.Error errors={toFieldErrors(shownErrors.name)} />
						</Field.Field>
					</div>
					<Field.Field data-invalid={!!shownErrors.description || undefined}>
						<Field.Label for="accounting-unit-description">Beschreibung</Field.Label>
						<CountedTextarea
							id="accounting-unit-description"
							bind:value={description}
							maxlength={DESCRIPTION_MAX_LENGTH}
							aria-invalid={!!shownErrors.description || undefined}
						/>
						<Field.Error errors={toFieldErrors(shownErrors.description)} />
					</Field.Field>
				</Field.Group>
			</Card.Content>
		</Card.Root>

		<UsersWithRolesList
			title="Benutzer und Rollen"
			emptyText="Noch keine Benutzer in diesem Haushalt"
			showAddUser={true}
			showEditUser={true}
			showDeleteUser={true}
			{users}
			roleKeyToName={(key) => accountingUnitRoleNames[key as AccountingUnitRole]}
			existingRoles={roles}
			editDialogDescription={(user) =>
				user
					? `Rollen von ${userCache.getDisplayName(user.username)} in diesem Haushalt. Die Änderungen werden mit „Speichern“ übernommen.`
					: 'Person suchen und ihre Rollen in diesem Haushalt auswählen. Die Änderungen werden mit „Speichern“ übernommen.'}
			hasRemoveDialog={false}
			removeDialogTitle={(user) => `${userCache.getDisplayName(user.username)} aus dem Haushalt entfernen?`}
			onEdit={handleEdit}
			onRemove={handleRemove}
		/>
	{/if}
</div>
