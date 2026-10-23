<script lang="ts">
	import { onDestroy } from 'svelte';
	import ChevronsUpDownIcon from '@lucide/svelte/icons/chevrons-up-down';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Command from '#lib/components/ui/command/index.js';
	import * as Popover from '#lib/components/ui/popover/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import apiClient from '#lib/api/client.js';
	import type { User } from '#lib/api/types.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';
	import { getUserDisplayName } from '#lib/texts/usernames.js';
	import { cn } from '#lib/type-utils.js';

	// Mirrors GetUserTask.SearchStringMinLength in the backend
	const MIN_SEARCH_LENGTH = 2;
	const SEARCH_DELAY_MS = 300;

	type SearchState = 'idle' | 'loading' | 'done' | 'failed';

	interface Props {
		value: User | undefined;
		onChange: (user: User | undefined) => void;
		/** User names that are left out of the results, e.g. because they were already added */
		excludeUsernames?: string[];
		placeholder?: string;
		class?: string;
		'aria-label'?: string;
	}

	let {
		value,
		onChange,
		excludeUsernames = [],
		placeholder = 'Name suchen…',
		class: className,
		'aria-label': ariaLabel
	}: Props = $props();

	let open = $state(false);
	let searchText = $state('');
	let userResults = $state<User[]>([]);
	let searchState = $state<SearchState>('idle');

	// Only the latest search may update the results, so a slow earlier response cannot overwrite a newer one
	let latestSearchNumber = 0;
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;
	onDestroy(() => clearTimeout(searchTimeout));

	async function search(text: string, searchNumber: number) {
		let foundUsers: User[] = [];
		let failed = false;
		try {
			const { data, response } = await apiClient.GET('/api/get-users/search-user', {
				params: { query: { search: text } }
			});
			if (response.ok && data) foundUsers = data;
			else failed = true;
		} catch {
			failed = true;
		}

		if (searchNumber !== latestSearchNumber) return;
		// The names of the results are cached, so the selected user is shown without loading it again
		userCache.cacheUsers(foundUsers);
		userResults = foundUsers;
		searchState = failed ? 'failed' : 'done';
	}

	// Waits until the typing pauses, so not every key press starts a request
	function handleSearch(text: string) {
		clearTimeout(searchTimeout);
		const searchNumber = ++latestSearchNumber;

		if (text.trim().length < MIN_SEARCH_LENGTH) {
			userResults = [];
			searchState = 'idle';
			return;
		}

		searchState = 'loading';
		searchTimeout = setTimeout(() => void search(text.trim(), searchNumber), SEARCH_DELAY_MS);
	}

	function select(user: User) {
		onChange(user);
		open = false;
		searchText = '';
		handleSearch('');
	}

	const options = $derived.by(() => {
		const excluded = new Set(excludeUsernames.map((username) => username.toLowerCase()));
		return userResults.filter((user) => !excluded.has(user.username.toLowerCase()));
	});

	const statusText = $derived(
		{
			idle: `Mindestens ${MIN_SEARCH_LENGTH} Zeichen eingeben`,
			loading: undefined,
			done: 'Keine Benutzer gefunden',
			failed: 'Suche fehlgeschlagen'
		}[searchState]
	);
</script>

<!-- Finds users by their real name, user name or email through the API's user search -->
<Popover.Root bind:open>
	<Popover.Trigger>
		{#snippet child({ props })}
			<Button
				{...props}
				variant="outline"
				role="combobox"
				aria-label={ariaLabel}
				class={cn('justify-between font-normal', className)}
			>
				<span class={cn('truncate', !value && 'text-muted-foreground')}>
					{value ? getUserDisplayName(value) : placeholder}
				</span>
				<ChevronsUpDownIcon class="opacity-50" />
			</Button>
		{/snippet}
	</Popover.Trigger>
	<Popover.Content class="w-(--bits-popover-anchor-width) min-w-64 p-0" align="start">
		<Command.Root shouldFilter={false}>
			<Command.Input
				placeholder="Name, Benutzername oder E-Mail"
				aria-label="Suchbegriff"
				bind:value={searchText}
				oninput={(event) => handleSearch(event.currentTarget.value)}
			/>
			<Command.List>
				{#if searchState === 'loading'}
					<div class="flex justify-center py-6">
						<Spinner aria-label="Suche läuft" />
					</div>
				{:else if options.length === 0}
					<div class="py-6 text-center text-sm text-muted-foreground">{statusText}</div>
				{:else}
					{#each options as user (user.username)}
						<Command.Item value={user.username} onSelect={() => select(user)}>
							{getUserDisplayName(user)}
						</Command.Item>
					{/each}
				{/if}
			</Command.List>
		</Command.Root>
	</Popover.Content>
</Popover.Root>
