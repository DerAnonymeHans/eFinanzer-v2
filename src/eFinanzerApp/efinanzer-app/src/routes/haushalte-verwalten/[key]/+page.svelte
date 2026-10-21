<script lang="ts" module>
	import type { AccountingUnitRole } from '#lib/api/types.js';
	import { accountingUnitRoleNames } from '#lib/texts/roles.js';

	const roles = Object.keys(accountingUnitRoleNames) as AccountingUnitRole[];
</script>

<script lang="ts">
	import { toast } from 'svelte-sonner';
	import ArrowLeftIcon from '@lucide/svelte/icons/arrow-left';
	import SaveIcon from '@lucide/svelte/icons/save';
	import Trash2Icon from '@lucide/svelte/icons/trash-2';
	import UserPlusIcon from '@lucide/svelte/icons/user-plus';
	import { resolve } from '$app/paths';
	import type { PageProps } from './$types';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as ButtonGroup from '#lib/components/ui/button-group/index.js';
	import * as Card from '#lib/components/ui/card/index.js';
	import * as Field from '#lib/components/ui/field/index.js';
	import { Input } from '#lib/components/ui/input/index.js';
	import * as Select from '#lib/components/ui/select/index.js';
	import CountedTextarea from '#lib/components/CountedTextarea.svelte';
	import EmptyState from '#lib/components/EmptyState.svelte';
	import ErrorAlert from '#lib/components/ErrorAlert.svelte';
	import PageSpinner from '#lib/components/PageSpinner.svelte';
	import UserName from '#lib/components/UserName.svelte';
	import UserSearchSelect from '#lib/components/UserSearchSelect.svelte';
	import apiClient from '#lib/api/client.js';
	import type { AccountingUnitDetails, User, UserRoles } from '#lib/api/types.js';
	import { confirm } from '#lib/confirm.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';
	import { emptyToNull, toFieldErrors, validateMaxLength } from '#lib/validation.js';
	import { DESCRIPTION_MAX_LENGTH, NAME_MAX_LENGTH } from '../accounting-unit-validation.js';

	let { params }: PageProps = $props();

	const access = useUserAccess();

	let accountingUnit = $state<AccountingUnitDetails>();
	let error = $state<string>();
	let name = $state('');
	let description = $state('');
	let users = $state<UserRoles[]>([]);
	let newUser = $state<User>();
	let submitted = $state(false);

	// Only the latest request may update the state, so a slow earlier response cannot overwrite a newer one
	let latestRequest = 0;

	async function load(key: string) {
		const request = ++latestRequest;

		let result: AccountingUnitDetails | undefined;
		let failure: string | undefined;
		try {
			const { data, response } = await apiClient.GET('/api/manage-accounting-units/get-accounting-unit', {
				params: { query: { key } }
			});
			if (response.ok && data) result = data;
			else if (response.status === 404) failure = `Den Haushalt „${key}“ gibt es nicht`;
			else failure = `Haushalt konnte nicht geladen werden (HTTP ${response.status})`;
		} catch {
			failure = 'Haushalt konnte nicht geladen werden';
		}

		if (request !== latestRequest) return;
		accountingUnit = result;
		error = failure;
		name = result?.name ?? '';
		description = result?.description ?? '';
		users = result?.usersWithRoles ?? [];
	}

	// Loads again when another Haushalt is opened
	$effect(() => {
		void load(params.key);
	});

	const errors = $derived({
		name: validateMaxLength(name, NAME_MAX_LENGTH),
		description: validateMaxLength(description, DESCRIPTION_MAX_LENGTH)
	});
	const shownErrors = $derived<Partial<typeof errors>>(submitted ? errors : {});

	const usersWithoutRoles = $derived(users.filter((user) => user.roles.length === 0));

	function removeUser(userName: string) {
		users = users.filter((user) => user.userName !== userName);
	}

	// The search leaves out users that are already listed, so the selected one can be added as it is
	function addUser() {
		if (!newUser) return;
		users.push({ userName: newUser.username, roles: [] });
		newUser = undefined;
	}

	async function save() {
		try {
			const { response } = await apiClient.PUT('/api/manage-accounting-units/update-accounting-unit', {
				params: { query: { key: params.key } },
				body: {
					name: emptyToNull(name),
					description: emptyToNull(description),
					usersWithRoles: $state.snapshot(users)
				}
			});

			if (response.ok) {
				toast.success('Änderungen wurden gespeichert');
				void load(params.key);
				// The side navigation shows the name of the Haushalt and the roles of the logged-in user
				void access.reload();
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig, eventuell gibt es einen Benutzer nicht');
			} else if (response.status === 404) {
				toast.error('Der Haushalt existiert nicht mehr');
			} else {
				// Role changes are applied one after another, so some of them may have been saved
				toast.error(`Änderungen konnten nicht vollständig gespeichert werden (HTTP ${response.status})`);
				void load(params.key);
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
	{#if usersWithoutRoles.length > 0}
		<p class="mt-2">
			Ohne Rolle und damit aus dem Haushalt entfernt werden:
			{#each usersWithoutRoles as user, index (user.userName)}
				{#if index > 0},
				{/if}<UserName username={user.userName} />
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

	{#if error}
		<ErrorAlert message={error} onRetry={() => void load(params.key)} />
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

		<Card.Root>
			<Card.Header>
				<Card.Title class="text-base">Benutzer und Rollen</Card.Title>
			</Card.Header>
			<Card.Content class="flex flex-col gap-3">
				{#if users.length === 0}
					<EmptyState description="Noch keine Benutzer in diesem Haushalt" />
				{/if}

				{#each users as user (user.userName)}
					{@const labelId = `roles-${user.userName}`}
					<!-- Shows the full name, the cryptic user name is only used as key and id -->
					<div
						class="grid grid-cols-[1fr_auto] items-center gap-3 md:grid-cols-[minmax(120px,200px)_1fr_auto]"
					>
						<span class="truncate font-semibold" id={labelId}><UserName username={user.userName} /></span>
						<div class="col-span-full row-start-2 md:col-span-1 md:row-start-auto">
							<Select.Root type="multiple" bind:value={user.roles}>
								<Select.Trigger class="w-full" aria-labelledby={labelId}>
									{#if user.roles.length === 0}
										<span class="text-muted-foreground">Keine Rolle</span>
									{:else}
										{roles
											.filter((role) => user.roles.includes(role))
											.map((role) => accountingUnitRoleNames[role])
											.join(', ')}
									{/if}
								</Select.Trigger>
								<Select.Content>
									{#each roles as role (role)}
										<Select.Item value={role} label={accountingUnitRoleNames[role]} />
									{/each}
								</Select.Content>
							</Select.Root>
						</div>
						<Button
							variant="ghost"
							size="icon"
							class="col-start-2 row-start-1 text-destructive hover:bg-destructive/10 hover:text-destructive md:col-start-auto"
							aria-label={`${userCache.getDisplayName(user.userName)} entfernen`}
							title="Entfernen"
							onclick={() => removeUser(user.userName)}
						>
							<Trash2Icon />
						</Button>
					</div>
				{/each}

				<ButtonGroup.Root class="mt-2 w-full max-w-[480px]">
					<UserSearchSelect
						class="min-w-0 flex-1"
						aria-label="Benutzer suchen"
						value={newUser}
						onChange={(user) => (newUser = user)}
						excludeUsernames={users.map((user) => user.userName)}
					/>
					<Button variant="outline" disabled={!newUser} onclick={addUser}>
						<UserPlusIcon />
						Hinzufügen
					</Button>
				</ButtonGroup.Root>
			</Card.Content>
		</Card.Root>
	{/if}
</div>
